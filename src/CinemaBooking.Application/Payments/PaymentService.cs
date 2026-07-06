using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Contracts.Payment;
using CinemaBooking.Application.Invoices;
using CinemaBooking.Application.Notifications;
using CinemaBooking.Application.Payments.VNPay;
using CinemaBooking.Application.Payments.PayOS;
using CinemaBooking.Application.Tickets;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Payments;

public sealed class PaymentService : IPaymentService
{
    private const int VNPaySessionExpiryMinutes = 15;
    private const int PayOSSessionExpiryMinutes = 15;
    private const int FailedPaymentHoldMinutes = 5;

    private readonly IPaymentRepository _paymentRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IVNPayService _vnpayService;
    private readonly IPayOSService _payOSService;
    private readonly IInvoiceService _invoiceService;
    private readonly ITicketService _ticketService;
    private readonly INotificationOutbox _notificationOutbox;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IWalletRepository walletRepository,
        IBookingRepository bookingRepository,
        IVNPayService vnpayService,
        IPayOSService payOSService,
        IInvoiceService invoiceService,
        ITicketService ticketService,
        INotificationOutbox notificationOutbox,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _walletRepository = walletRepository;
        _bookingRepository = bookingRepository;
        _vnpayService = vnpayService;
        _payOSService = payOSService;
        _invoiceService = invoiceService;
        _ticketService = ticketService;
        _notificationOutbox = notificationOutbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentOperationResult> InitiatePaymentAsync(
        InitiatePaymentRequest request,
        int actorUserId,
        bool isStaff,
        string ipAddress = "127.0.0.1",
        CancellationToken cancellationToken = default)
    {
        var method = EnumValueMapper.Validate(
            request.PaymentMethod, "PaymentMethod", DatabaseEnumMappings.PaymentMethods);
        if (!method.Succeeded)
            return Error(PaymentErrorType.Validation, method.ErrorMessage!);

        var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId, cancellationToken);
        var accessError = ValidateBookingForPayment(booking, actorUserId, isStaff);
        if (accessError is not null)
            return accessError;

        var existingPayment = await _paymentRepository.GetPaymentByBookingIdAsync(
            request.BookingId, cancellationToken);
        var isRetry = existingPayment is not null
            && existingPayment.Status == PaymentStatus.Failed
            && booking!.Status == BookingStatus.PaymentFailed;
        if (existingPayment is not null && !isRetry)
            return Error(PaymentErrorType.Conflict, "Payment already exists for this booking.");

        if (isRetry && !await _bookingRepository.HasActiveBookingHoldsAsync(
                booking!.BookingID, DateTime.UtcNow, cancellationToken))
            return Error(PaymentErrorType.Conflict, "The payment retry window has expired. Please reselect seats.");

        return method.DatabaseValue switch
        {
            PaymentMethod.Wallet => await ProcessWalletPaymentAsync(booking!, existingPayment, cancellationToken),
            PaymentMethod.VNPay => await ProcessVNPayPaymentAsync(booking!, existingPayment, ipAddress, cancellationToken),
            PaymentMethod.PayOS => await ProcessPayOSPaymentAsync(booking!, existingPayment, cancellationToken),
            PaymentMethod.Cash => await ProcessCashPaymentAsync(booking!, existingPayment, cancellationToken),
            _ => Error(PaymentErrorType.Validation, method.ErrorMessage ?? "Payment method is not supported.")
        };
    }

    public async Task<PaymentOperationResult> ConfirmCashPaymentAsync(
        ConfirmCashPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Error(PaymentErrorType.NotFound, "Payment not found.");
        if (payment.Status != PaymentStatus.Pending)
            return Error(PaymentErrorType.Conflict, "Payment is not pending.");
        if (payment.PaymentMethod != PaymentMethod.Cash)
            return Error(PaymentErrorType.Conflict, "Payment is not a cash payment.");

        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await CompletePaymentAsync(payment, null, cancellationToken);
            var updated = await _paymentRepository.GetPaymentByIdAsync(request.PaymentId, cancellationToken);
            return PaymentOperationResult.Success(MapToPaymentResponse(updated)!);
        }, cancellationToken);
        await _notificationOutbox.EnqueueBookingSuccessAsync(payment.BookingID, cancellationToken);
        return result;
    }

    public async Task<VNPayCallbackResult> ProcessVNPayCallbackAsync(
        Dictionary<string, string> vnpayData,
        CancellationToken cancellationToken = default)
    {
        var (isValid, responseCode, transactionNo) = await _vnpayService.ProcessCallbackAsync(
            vnpayData, cancellationToken);
        if (!isValid)
            return new(false, "Invalid signature from VNPay");
        if (!vnpayData.TryGetValue("vnp_TxnRef", out var txnRef))
            return new(false, "Missing transaction reference");

        var paymentId = ExtractPaymentIdFromTxnRef(txnRef);
        if (paymentId == 0)
            return new(false, $"Invalid transaction reference format: {txnRef}");

        var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId, cancellationToken);
        if (payment is null)
            return new(false, $"Payment {paymentId} not found");
        if (payment.Status != PaymentStatus.Pending)
            return new(true, "Payment already processed", payment.PaymentID, payment.BookingID,
                EnumValueMapper.ToApiValue(payment.Status));

        var isSuccess = responseCode == VNPayResponseCode.Success;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (isSuccess)
            {
                await CompletePaymentAsync(payment, transactionNo, cancellationToken);
                await _paymentRepository.UpdatePaymentSessionsForPaymentAsync(
                    paymentId, "completed", cancellationToken);
            }
            else
            {
                await _paymentRepository.UpdatePaymentStatusAsync(
                    paymentId, PaymentStatus.Failed, null, transactionNo, cancellationToken);
                await _bookingRepository.UpdateBookingStatusAsync(
                    payment.BookingID, BookingStatus.PaymentFailed, cancellationToken);
                await _bookingRepository.ExtendBookingHoldsAsync(
                    payment.BookingID, DateTime.UtcNow.AddMinutes(FailedPaymentHoldMinutes), cancellationToken);
                await _paymentRepository.UpdatePaymentSessionsForPaymentAsync(
                    paymentId, "expired", cancellationToken);
            }

            return true;
        }, cancellationToken);

        if (isSuccess)
            await _notificationOutbox.EnqueueBookingSuccessAsync(payment.BookingID, cancellationToken);

        var bookingStatus = isSuccess ? BookingStatus.Paid : BookingStatus.PaymentFailed;
        var paymentStatus = isSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;
        return new(true,
            isSuccess ? "Payment completed successfully" : $"Payment failed with code: {responseCode}",
            payment.PaymentID, payment.BookingID, EnumValueMapper.ToApiValue(paymentStatus),
            EnumValueMapper.ToApiValue(bookingStatus));
    }

    public async Task<PayOSWebhookResult> ProcessPayOSWebhookAsync(
        PayOSWebhook webhook,
        CancellationToken cancellationToken = default)
    {
        PayOSVerifiedWebhookData verified;
        try
        {
            verified = await _payOSService.VerifyWebhookAsync(webhook, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(false, "Invalid signature from PayOS.");
        }

        if (verified.OrderCode <= 0)
            return new(false, $"Invalid PayOS order code: {verified.OrderCode}.");

        var paymentSession = await _paymentRepository.GetPaymentSessionByOrderNoAsync(
            verified.OrderCode.ToString(), cancellationToken);
        if (paymentSession is null || paymentSession.GatewayName != PaymentMethod.PayOS)
            return new(false, $"PayOS order {verified.OrderCode} not found.");

        var paymentId = paymentSession.PaymentID;
        var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId, cancellationToken);
        if (payment is null)
            return new(false, $"Payment {paymentId} not found.");
        if (payment.PaymentMethod != PaymentMethod.PayOS)
            return new(false, "PayOS order code does not belong to a PayOS payment.");
        if (payment.Amount != verified.Amount)
            return new(false, "PayOS payment amount does not match the booking amount.");
        if (payment.Status != PaymentStatus.Pending)
            return new(true, "Payment already processed.", payment.PaymentID, payment.BookingID,
                EnumValueMapper.ToApiValue(payment.Status),
                EnumValueMapper.ToApiValue(payment.Booking.Status));

        if (verified.Code != "00")
            return new(true, $"PayOS webhook acknowledged with code {verified.Code}.",
                payment.PaymentID, payment.BookingID,
                EnumValueMapper.ToApiValue(payment.Status),
                EnumValueMapper.ToApiValue(payment.Booking.Status));

        var completed = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _paymentRepository.TryCompletePendingPaymentAsync(
                    paymentId, DateTime.UtcNow, verified.Reference, cancellationToken))
                return false;

            await FinalizeSuccessfulBookingAsync(payment.Booking, cancellationToken);
            await _invoiceService.CreateInvoiceAsync(payment.BookingID, cancellationToken);
            await _paymentRepository.UpdatePaymentSessionsForPaymentAsync(
                paymentId, "completed", cancellationToken);
            return true;
        }, cancellationToken);

        if (!completed)
            return new(true, "Payment already processed.", payment.PaymentID, payment.BookingID,
                EnumValueMapper.ToApiValue(PaymentStatus.Completed),
                EnumValueMapper.ToApiValue(BookingStatus.Paid));

        await _notificationOutbox.EnqueueBookingSuccessAsync(payment.BookingID, cancellationToken);

        return new(true, "Payment completed successfully.", payment.PaymentID, payment.BookingID,
            EnumValueMapper.ToApiValue(PaymentStatus.Completed),
            EnumValueMapper.ToApiValue(BookingStatus.Paid));
    }

    public async Task<PaymentOperationResult> GetPaymentByIdAsync(
        int paymentId,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId, cancellationToken);
        var accessResult = MapAccessiblePayment(payment, actorUserId, isStaff);
        if (!accessResult.Succeeded)
            return accessResult;

        payment = await SynchronizePendingPayOSPaymentAsync(payment, cancellationToken);
        return MapAccessiblePayment(payment, actorUserId, isStaff);
    }

    public async Task<PaymentOperationResult> GetPaymentByBookingIdAsync(
        int bookingId,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByBookingIdAsync(bookingId, cancellationToken);
        var accessResult = MapAccessiblePayment(payment, actorUserId, isStaff);
        if (!accessResult.Succeeded)
            return accessResult;

        payment = await SynchronizePendingPayOSPaymentAsync(payment, cancellationToken);
        return MapAccessiblePayment(payment, actorUserId, isStaff);
    }

    private async Task<Payment?> SynchronizePendingPayOSPaymentAsync(
        Payment? payment,
        CancellationToken cancellationToken)
    {
        if (payment is null
            || payment.PaymentMethod != PaymentMethod.PayOS
            || payment.Status != PaymentStatus.Pending)
            return payment;

        var session = await _paymentRepository.GetLatestPaymentSessionAsync(
            payment.PaymentID, cancellationToken);
        if (session?.GatewayOrderNo is null
            || !long.TryParse(session.GatewayOrderNo, out var orderCode))
            return payment;

        PayOSPaymentLinkStatusResult paymentLink;
        try
        {
            paymentLink = await _payOSService.GetPaymentLinkStatusAsync(orderCode, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return payment;
        }

        if (!string.Equals(paymentLink.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            return payment;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _paymentRepository.TryCancelPendingPaymentAsync(
                    payment.PaymentID, cancellationToken))
                return false;

            await _bookingRepository.UpdateBookingStatusAsync(
                payment.BookingID, BookingStatus.Cancelled, cancellationToken);
            await _paymentRepository.UpdatePaymentSessionsForPaymentAsync(
                payment.PaymentID, "cancelled", cancellationToken);
            return true;
        }, cancellationToken);

        return await _paymentRepository.GetPaymentByIdAsync(payment.PaymentID, cancellationToken);
    }

    private async Task<PaymentOperationResult> ProcessWalletPaymentAsync(
        Booking booking,
        Payment? existingPayment,
        CancellationToken cancellationToken)
    {
        if (!booking.UserID.HasValue)
            return Error(PaymentErrorType.Validation, "Guest bookings cannot use wallet payment.");

        if (!await _walletRepository.CheckSufficientBalanceAsync(
                booking.UserID.Value, booking.FinalAmount, cancellationToken))
            return Error(PaymentErrorType.Validation, "Insufficient wallet balance.");

        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _walletRepository.DeductBalanceAsync(
                booking.UserID.Value, booking.FinalAmount, cancellationToken);
            Payment payment;
            if (existingPayment is null)
            {
                payment = await _paymentRepository.CreatePaymentAsync(new Payment
                {
                    BookingID = booking.BookingID,
                    PaymentMethod = PaymentMethod.Wallet,
                    Amount = booking.FinalAmount,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                payment = existingPayment;
                await _paymentRepository.ResetPaymentForRetryAsync(
                    payment.PaymentID, PaymentMethod.Wallet, booking.FinalAmount, cancellationToken);
                await _bookingRepository.UpdateBookingStatusAsync(
                    booking.BookingID, BookingStatus.Pending, cancellationToken);
            }

            await _paymentRepository.UpdatePaymentStatusAsync(
                payment.PaymentID, PaymentStatus.Completed, DateTime.UtcNow, null, cancellationToken);

            var wallet = await _walletRepository.GetWalletByUserIdAsync(booking.UserID.Value, cancellationToken)
                ?? throw new InvalidOperationException("Wallet disappeared during payment.");
            await _walletRepository.CreateTransactionAsync(new WalletTransaction
            {
                WalletID = wallet.WalletID,
                Amount = -booking.FinalAmount,
                BalanceAfter = wallet.Balance,
                TransactionType = WalletTransactionType.Payment,
                BookingID = booking.BookingID,
                Description = $"Payment for booking {booking.BookingCode}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await FinalizeSuccessfulBookingAsync(booking, cancellationToken);
            var invoice = await _invoiceService.CreateInvoiceAsync(booking.BookingID, cancellationToken);
            return PaymentOperationResult.Success(new WalletPaymentResponse(
                true, payment.PaymentID, booking.BookingID,
                EnumValueMapper.ToApiValue(PaymentMethod.Wallet), booking.FinalAmount,
                EnumValueMapper.ToApiValue(PaymentStatus.Completed),
                EnumValueMapper.ToApiValue(BookingStatus.Paid), invoice.InvoiceCode,
                CalculatePointsEarned(booking.FinalAmount)));
        }, cancellationToken);
        await _notificationOutbox.EnqueueBookingSuccessAsync(booking.BookingID, cancellationToken);
        return result;
    }

    private async Task<PaymentOperationResult> ProcessVNPayPaymentAsync(
        Booking booking,
        Payment? existingPayment,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Payment payment;
            if (existingPayment is null)
            {
                payment = await _paymentRepository.CreatePaymentAsync(new Payment
                {
                    BookingID = booking.BookingID,
                    PaymentMethod = PaymentMethod.VNPay,
                    Amount = booking.FinalAmount,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                payment = existingPayment;
                await _paymentRepository.ResetPaymentForRetryAsync(
                    payment.PaymentID, PaymentMethod.VNPay, booking.FinalAmount, cancellationToken);
                await _bookingRepository.UpdateBookingStatusAsync(
                    booking.BookingID, BookingStatus.Pending, cancellationToken);
            }
            var expiresAt = DateTime.UtcNow.AddMinutes(VNPaySessionExpiryMinutes);
            var url = await _vnpayService.CreatePaymentUrlAsync(payment, booking, ipAddress, cancellationToken);
            var session = await _paymentRepository.CreatePaymentSessionAsync(new PaymentSession
            {
                PaymentID = payment.PaymentID,
                GatewayName = PaymentMethod.VNPay,
                QRCodeURL = url,
                ExpiresAt = expiresAt,
                Status = "waiting",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            return PaymentOperationResult.Success(new VNPayPaymentResponse(
                true, payment.PaymentID, booking.BookingID,
                EnumValueMapper.ToApiValue(PaymentMethod.VNPay), booking.FinalAmount,
                EnumValueMapper.ToApiValue(PaymentStatus.Pending), url, session.SessionID, expiresAt));
        }, cancellationToken);
    }

    private async Task<PaymentOperationResult> ProcessCashPaymentAsync(
        Booking booking,
        Payment? existingPayment,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Payment payment;
            if (existingPayment is null)
            {
                payment = await _paymentRepository.CreatePaymentAsync(new Payment
                {
                    BookingID = booking.BookingID,
                    PaymentMethod = PaymentMethod.Cash,
                    Amount = booking.FinalAmount,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                payment = existingPayment;
                await _paymentRepository.ResetPaymentForRetryAsync(
                    payment.PaymentID, PaymentMethod.Cash, booking.FinalAmount, cancellationToken);
                await _bookingRepository.UpdateBookingStatusAsync(
                    booking.BookingID, BookingStatus.Pending, cancellationToken);
            }
            return PaymentOperationResult.Success(new CashPaymentResponse(
                true, payment.PaymentID, booking.BookingID,
                EnumValueMapper.ToApiValue(PaymentMethod.Cash), booking.FinalAmount,
                EnumValueMapper.ToApiValue(PaymentStatus.Pending),
                "Please pay at the counter before the showtime"));
        }, cancellationToken);
    }

    private async Task<PaymentOperationResult> ProcessPayOSPaymentAsync(
        Booking booking,
        Payment? existingPayment,
        CancellationToken cancellationToken)
    {
        if (booking.FinalAmount != decimal.Truncate(booking.FinalAmount)
            || booking.FinalAmount is <= 0 or > int.MaxValue)
            return Error(PaymentErrorType.Validation,
                "PayOS requires a positive whole-number VND amount within the supported range.");

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                Payment payment;
                if (existingPayment is null)
                {
                    payment = await _paymentRepository.CreatePaymentAsync(new Payment
                    {
                        BookingID = booking.BookingID,
                        PaymentMethod = PaymentMethod.PayOS,
                        Amount = booking.FinalAmount,
                        Status = PaymentStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
                else
                {
                    payment = existingPayment;
                    await _paymentRepository.ResetPaymentForRetryAsync(
                        payment.PaymentID, PaymentMethod.PayOS, booking.FinalAmount, cancellationToken);
                    await _bookingRepository.UpdateBookingStatusAsync(
                        booking.BookingID, BookingStatus.Pending, cancellationToken);
                }

                var expiresAt = DateTime.UtcNow.AddMinutes(PayOSSessionExpiryMinutes);
                var orderCode = CreatePayOSOrderCode();
                var paymentLink = await _payOSService.CreatePaymentLinkAsync(
                    orderCode,
                    decimal.ToInt32(booking.FinalAmount),
                    $"PAY {payment.PaymentID}",
                    expiresAt,
                    cancellationToken);
                var session = await _paymentRepository.CreatePaymentSessionAsync(new PaymentSession
                {
                    PaymentID = payment.PaymentID,
                    GatewayName = PaymentMethod.PayOS,
                    GatewayOrderNo = paymentLink.OrderCode.ToString(),
                    QRCodeURL = paymentLink.CheckoutUrl,
                    ExpiresAt = expiresAt,
                    Status = "waiting",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                return PaymentOperationResult.Success(new PayOSPaymentResponse(
                    true,
                    payment.PaymentID,
                    booking.BookingID,
                    EnumValueMapper.ToApiValue(PaymentMethod.PayOS),
                    booking.FinalAmount,
                    EnumValueMapper.ToApiValue(PaymentStatus.Pending),
                    paymentLink.CheckoutUrl,
                    paymentLink.QrCode,
                    paymentLink.PaymentLinkId,
                    paymentLink.OrderCode,
                    session.SessionID,
                    expiresAt));
            }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Error(PaymentErrorType.Gateway,
                "Unable to create PayOS payment link. Please try again later.");
        }
    }

    private async Task CompletePaymentAsync(
        Payment payment,
        string? transactionCode,
        CancellationToken cancellationToken)
    {
        await _paymentRepository.UpdatePaymentStatusAsync(
            payment.PaymentID, PaymentStatus.Completed, DateTime.UtcNow,
            transactionCode, cancellationToken);
        await FinalizeSuccessfulBookingAsync(payment.Booking, cancellationToken);
        await _invoiceService.CreateInvoiceAsync(payment.BookingID, cancellationToken);
    }

    private async Task FinalizeSuccessfulBookingAsync(Booking booking, CancellationToken cancellationToken)
    {
        await _bookingRepository.UpdateBookingStatusAsync(
            booking.BookingID, BookingStatus.Paid, cancellationToken);

        await _ticketService.CreateTicketsForBookingAsync(booking.BookingID, cancellationToken);
    }

    private static PaymentOperationResult? ValidateBookingForPayment(
        Booking? booking,
        int actorUserId,
        bool isStaff)
    {
        if (booking is null)
            return Error(PaymentErrorType.NotFound, "Booking not found.");
        if (!isStaff && booking.UserID != actorUserId)
            return Error(PaymentErrorType.Forbidden, "You cannot access another customer's booking.");
        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.PaymentFailed)
            return Error(PaymentErrorType.Conflict, "Booking is not pending.");
        return null;
    }

    private static PaymentOperationResult MapAccessiblePayment(
        Payment? payment,
        int actorUserId,
        bool isStaff)
    {
        if (payment is null)
            return Error(PaymentErrorType.NotFound, "Payment not found.");
        if (!isStaff && payment.Booking.UserID != actorUserId)
            return Error(PaymentErrorType.Forbidden, "You cannot access another customer's payment.");
        return PaymentOperationResult.Success(MapToPaymentResponse(payment)!);
    }

    private static PaymentOperationResult Error(PaymentErrorType type, string message) =>
        PaymentOperationResult.Failure(type, message);

    private static PaymentResponse? MapToPaymentResponse(Payment? payment) => payment is null
        ? null
        : new(payment.PaymentID, payment.BookingID,
            EnumValueMapper.ToApiValue(payment.PaymentMethod), payment.Amount,
            EnumValueMapper.ToApiValue(payment.Status), payment.PaidAt, payment.CreatedAt);

    private static int CalculatePointsEarned(decimal amount) => (int)(amount / 1000m);

    private static int ExtractPaymentIdFromTxnRef(string txnRef)
    {
        var parts = txnRef.Split('_');
        return parts.Length >= 2 && int.TryParse(parts[1], out var paymentId) ? paymentId : 0;
    }

    private static long CreatePayOSOrderCode() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 1_000_000L
        + Random.Shared.Next(1, 1_000_000);
}
