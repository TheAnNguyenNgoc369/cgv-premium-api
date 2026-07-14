using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Contracts.Payment;
using CinemaBooking.Application.Invoices;
using CinemaBooking.Application.Notifications;
using CinemaBooking.Application.Payments.PayOS;
using CinemaBooking.Application.Tickets;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Payments;

public sealed class PaymentService : IPaymentService
{
    private const int PayOSSessionExpiryMinutes = 15;
    private const int FailedPaymentHoldMinutes = 5;

    private readonly IPaymentRepository _paymentRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPayOSService _payOSService;
    private readonly IInvoiceService _invoiceService;
    private readonly ITicketService _ticketService;
    private readonly INotificationOutbox _notificationOutbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly IVoucherRepository _voucherRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IWalletRepository walletRepository,
        IBookingRepository bookingRepository,
        IPayOSService payOSService,
        IInvoiceService invoiceService,
        ITicketService ticketService,
        INotificationOutbox notificationOutbox,
        IUnitOfWork unitOfWork,
        IUserVoucherRepository userVoucherRepository,
        IVoucherRepository voucherRepository)
    {
        _paymentRepository = paymentRepository;
        _walletRepository = walletRepository;
        _bookingRepository = bookingRepository;
        _payOSService = payOSService;
        _invoiceService = invoiceService;
        _ticketService = ticketService;
        _notificationOutbox = notificationOutbox;
        _unitOfWork = unitOfWork;
        _userVoucherRepository = userVoucherRepository;
        _voucherRepository = voucherRepository;
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
        var accessError = await ValidateBookingForPaymentAsync(
            booking, actorUserId, isStaff, cancellationToken);
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
            PaymentMethod.PayOS => await ProcessPayOSPaymentAsync(booking!, existingPayment, cancellationToken),
            PaymentMethod.Cash => await ProcessCashPaymentAsync(booking!, existingPayment, cancellationToken),
            _ => Error(PaymentErrorType.Validation, method.ErrorMessage ?? "Payment method is not supported.")
        };
    }

    public async Task<PaymentOperationResult> ConfirmCashPaymentAsync(
        ConfirmCashPaymentRequest request,
        int staffUserId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Error(PaymentErrorType.NotFound, "Payment not found.");
        if (payment.Status != PaymentStatus.Pending)
            return Error(PaymentErrorType.Conflict, "Payment is not pending.");
        if (payment.PaymentMethod != PaymentMethod.Cash)
            return Error(PaymentErrorType.Conflict, "Payment is not a cash payment.");

        var accessError = await ValidateStaffCinemaAccessAsync(
            payment.Booking, staffUserId, cancellationToken);
        if (accessError is not null)
            return accessError;

        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await CompletePaymentAsync(payment, null, cancellationToken))
                return Error(PaymentErrorType.Conflict, "Payment is not pending.");

            var updated = await _paymentRepository.GetPaymentByIdAsync(request.PaymentId, cancellationToken);
            return PaymentOperationResult.Success(MapToPaymentResponse(updated)!);
        }, cancellationToken);

        if (result.Succeeded)
            await _notificationOutbox.EnqueueBookingSuccessAsync(payment.BookingID, cancellationToken);

        return result;
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
        var accessResult = await MapAccessiblePaymentAsync(
            payment, actorUserId, isStaff, cancellationToken);
        if (!accessResult.Succeeded)
            return accessResult;

        payment = await SynchronizePendingPayOSPaymentAsync(payment, cancellationToken);
        return await MapAccessiblePaymentAsync(payment, actorUserId, isStaff, cancellationToken);
    }

    public async Task<PaymentOperationResult> GetPaymentByBookingIdAsync(
        int bookingId,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByBookingIdAsync(bookingId, cancellationToken);
        var accessResult = await MapAccessiblePaymentAsync(
            payment, actorUserId, isStaff, cancellationToken);
        if (!accessResult.Succeeded)
            return accessResult;

        payment = await SynchronizePendingPayOSPaymentAsync(payment, cancellationToken);
        return await MapAccessiblePaymentAsync(payment, actorUserId, isStaff, cancellationToken);
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
            await _userVoucherRepository.ReleaseReservedByBookingAsync(
                payment.BookingID, cancellationToken);
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

        var transactionTime = DateTime.UtcNow;
        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _walletRepository.TryDeductBalanceAsync(
                    booking.UserID.Value, booking.FinalAmount, cancellationToken))
                return Error(PaymentErrorType.Validation, "Insufficient wallet balance.");

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
                Description = $"Wallet payment for booking {booking.BookingCode}",
                CreatedAt = transactionTime
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
        await _notificationOutbox.EnqueueWalletPaymentAsync(
            booking.UserID.Value, booking.FinalAmount, transactionTime, cancellationToken);
        return result;
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
                    booking.BookingID,
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

    private async Task<bool> CompletePaymentAsync(
        Payment payment,
        string? transactionCode,
        CancellationToken cancellationToken)
    {
        if (!await _paymentRepository.TryCompletePendingPaymentAsync(
                payment.PaymentID, DateTime.UtcNow, transactionCode, cancellationToken))
            return false;

        await FinalizeSuccessfulBookingAsync(payment.Booking, cancellationToken);
        await _invoiceService.CreateInvoiceAsync(payment.BookingID, cancellationToken);
        return true;
    }

    private async Task FinalizeSuccessfulBookingAsync(Booking booking, CancellationToken cancellationToken)
    {
        await _bookingRepository.UpdateBookingStatusAsync(
            booking.BookingID, BookingStatus.Paid, cancellationToken);

        // Public voucher: bump the global UsedCount now that the booking is Paid. The repo
        // filters on IsRedeemable = 0 so loyalty vouchers (which are counted at redeem time)
        // are untouched — and so is any booking without a voucher.
        await _voucherRepository.IncrementPublicVoucherUsageForBookingAsync(
            booking.BookingID, cancellationToken);

        // Redeemable voucher: reserved -> used. No-op for non-redeemable bookings.
        await _userVoucherRepository.MarkReservedAsUsedByBookingAsync(
            booking.BookingID, DateTime.UtcNow, cancellationToken);

        await _ticketService.CreateTicketsForBookingAsync(booking.BookingID, cancellationToken);

        var qrCode = GenerateQRCode();
        await _bookingRepository.UpdateBookingQRCodeAsync(booking.BookingID, qrCode, cancellationToken);
    }

    private static string GenerateQRCode()
    {
        return Guid.NewGuid().ToString("N").ToUpperInvariant();
    }

    private async Task<PaymentOperationResult?> ValidateBookingForPaymentAsync(
        Booking? booking,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken)
    {
        if (booking is null)
            return Error(PaymentErrorType.NotFound, "Booking not found.");
        if (!isStaff && booking.UserID != actorUserId)
            return Error(PaymentErrorType.Forbidden, "You cannot access another customer's booking.");
        if (isStaff)
        {
            var accessError = await ValidateStaffCinemaAccessAsync(
                booking, actorUserId, cancellationToken);
            if (accessError is not null)
                return accessError;
        }
        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.PaymentFailed)
            return Error(PaymentErrorType.Conflict, "Booking is not pending.");
        return null;
    }

    private async Task<PaymentOperationResult> MapAccessiblePaymentAsync(
        Payment? payment,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken)
    {
        if (payment is null)
            return Error(PaymentErrorType.NotFound, "Payment not found.");
        if (!isStaff && payment.Booking.UserID != actorUserId)
            return Error(PaymentErrorType.Forbidden, "You cannot access another customer's payment.");
        if (isStaff)
        {
            var accessError = await ValidateStaffCinemaAccessAsync(
                payment.Booking, actorUserId, cancellationToken);
            if (accessError is not null)
                return accessError;
        }
        return PaymentOperationResult.Success(MapToPaymentResponse(payment)!);
    }

    private async Task<PaymentOperationResult?> ValidateStaffCinemaAccessAsync(
        Booking booking,
        int staffUserId,
        CancellationToken cancellationToken)
    {
        var staffCinemaId = await _bookingRepository.GetStaffCinemaIdAsync(staffUserId, cancellationToken);
        if (!staffCinemaId.HasValue
            || booking.Showtime.Room.CinemaID != staffCinemaId.Value)
            return Error(PaymentErrorType.Forbidden, "You cannot access bookings outside your assigned cinema.");

        return null;
    }

    private static PaymentOperationResult Error(PaymentErrorType type, string message) =>
        PaymentOperationResult.Failure(type, message);

    private static PaymentResponse? MapToPaymentResponse(Payment? payment) => payment is null
        ? null
        : new(payment.PaymentID, payment.BookingID,
            EnumValueMapper.ToApiValue(payment.PaymentMethod), payment.Amount,
            EnumValueMapper.ToApiValue(payment.Status), payment.PaidAt, payment.CreatedAt);

    private static int CalculatePointsEarned(decimal amount) => (int)(amount / 1000m);

    private static long CreatePayOSOrderCode() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 1_000_000L
        + Random.Shared.Next(1, 1_000_000);
}
