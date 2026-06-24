using CinemaBooking.Application.Contracts.Payment;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Invoices;
using CinemaBooking.Application.Payments.VNPay;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Payments;

public sealed class PaymentService : IPaymentService
{
    private const int VNPaySessionExpiryMinutes = 15;
    private const decimal PointsPerThousand = 1m;

    private readonly IPaymentRepository _paymentRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IVNPayService _vnpayService;
    private readonly IInvoiceService _invoiceService;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IWalletRepository walletRepository,
        IBookingRepository bookingRepository,
        IVNPayService vnpayService,
        IInvoiceService invoiceService)
    {
        _paymentRepository = paymentRepository;
        _walletRepository = walletRepository;
        _bookingRepository = bookingRepository;
        _vnpayService = vnpayService;
        _invoiceService = invoiceService;
    }

    public async Task<object> InitiatePaymentAsync(
        InitiatePaymentRequest request,
        string ipAddress = "127.0.0.1",
        CancellationToken cancellationToken = default)
    {
        return request.PaymentMethod.ToLowerInvariant() switch
        {
            PaymentMethod.Wallet => await ProcessWalletPaymentAsync(request.BookingId, cancellationToken),
            PaymentMethod.VNPay => await ProcessVNPayPaymentAsync(request.BookingId, ipAddress, cancellationToken),
            PaymentMethod.Cash => await ProcessCashPaymentAsync(request.BookingId, cancellationToken),
            _ => throw new ArgumentException($"Invalid payment method: {request.PaymentMethod}")
        };
    }

    public async Task<PaymentResponse?> ConfirmCashPaymentAsync(
        ConfirmCashPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            throw new InvalidOperationException($"Payment {request.PaymentId} not found");

        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Payment {request.PaymentId} is not pending");

        if (payment.PaymentMethod != PaymentMethod.Cash)
            throw new InvalidOperationException($"Payment {request.PaymentId} is not a cash payment");

        await _paymentRepository.UpdatePaymentStatusAsync(
            request.PaymentId,
            PaymentStatus.Completed,
            DateTime.Now,
            null,
            cancellationToken);

        await _bookingRepository.UpdateBookingStatusAsync(
            payment.BookingID,
            BookingStatus.Paid,
            cancellationToken);

        await _invoiceService.CreateInvoiceAsync(payment.BookingID, cancellationToken);

        var updatedPayment = await _paymentRepository.GetPaymentByIdAsync(request.PaymentId, cancellationToken);
        return MapToPaymentResponse(updatedPayment);
    }

    public async Task<VNPayCallbackResult> ProcessVNPayCallbackAsync(
        Dictionary<string, string> vnpayData,
        CancellationToken cancellationToken = default)
    {
        var (isValid, responseCode, transactionNo) = await _vnpayService.ProcessCallbackAsync(
            vnpayData,
            cancellationToken);

        if (!isValid)
        {
            return new VNPayCallbackResult(
                Success: false,
                Message: "Invalid signature from VNPay");
        }

        if (!vnpayData.TryGetValue("vnp_TxnRef", out var txnRef))
        {
            return new VNPayCallbackResult(
                Success: false,
                Message: "Missing transaction reference");
        }

        var paymentId = ExtractPaymentIdFromTxnRef(txnRef);
        if (paymentId == 0)
        {
            return new VNPayCallbackResult(
                Success: false,
                Message: $"Invalid transaction reference format: {txnRef}");
        }

        var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return new VNPayCallbackResult(
                Success: false,
                Message: $"Payment {paymentId} not found");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return new VNPayCallbackResult(
                Success: true,
                Message: "Payment already processed",
                PaymentId: payment.PaymentID,
                BookingId: payment.BookingID,
                PaymentStatus: payment.Status);
        }

        var isSuccess = responseCode == VNPayResponseCode.Success;
        var newPaymentStatus = isSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;
        var newBookingStatus = isSuccess ? BookingStatus.Paid : BookingStatus.PaymentFailed;

        await _paymentRepository.UpdatePaymentStatusAsync(
            paymentId,
            newPaymentStatus,
            isSuccess ? DateTime.Now : null,
            transactionNo,
            cancellationToken);

        await _bookingRepository.UpdateBookingStatusAsync(
            payment.BookingID,
            newBookingStatus,
            cancellationToken);

        if (isSuccess)
        {
            await _invoiceService.CreateInvoiceAsync(payment.BookingID, cancellationToken);
        }

        return new VNPayCallbackResult(
            Success: true,
            Message: isSuccess ? "Payment completed successfully" : $"Payment failed with code: {responseCode}",
            PaymentId: payment.PaymentID,
            BookingId: payment.BookingID,
            PaymentStatus: newPaymentStatus,
            BookingStatus: newBookingStatus);
    }

    public async Task<PaymentResponse?> GetPaymentByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId, cancellationToken);
        return MapToPaymentResponse(payment);
    }

    public async Task<PaymentResponse?> GetPaymentByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetPaymentByBookingIdAsync(bookingId, cancellationToken);
        return MapToPaymentResponse(payment);
    }

    private async Task<WalletPaymentResponse> ProcessWalletPaymentAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking is null)
            throw new InvalidOperationException($"Booking {bookingId} not found");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Booking {bookingId} is not pending");

        var existingPayment = await _paymentRepository.GetPaymentByBookingIdAsync(bookingId, cancellationToken);
        if (existingPayment is not null)
            throw new InvalidOperationException($"Payment already exists for booking {bookingId}");

        var hasSufficientBalance = await _walletRepository.CheckSufficientBalanceAsync(
            booking.UserID,
            booking.FinalAmount,
            cancellationToken);

        if (!hasSufficientBalance)
            throw new InvalidOperationException("Insufficient wallet balance");

        await _walletRepository.DeductBalanceAsync(
            booking.UserID,
            booking.FinalAmount,
            cancellationToken);

        var payment = await _paymentRepository.CreatePaymentAsync(
            new Payment
            {
                BookingID = bookingId,
                PaymentMethod = PaymentMethod.Wallet,
                Amount = booking.FinalAmount,
                Status = PaymentStatus.Completed,
                PaidAt = DateTime.Now,
                CreatedAt = DateTime.Now
            },
            cancellationToken);

        var wallet = await _walletRepository.GetWalletByUserIdAsync(booking.UserID, cancellationToken);
        if (wallet is null)
            throw new InvalidOperationException($"Wallet not found for user {booking.UserID}");

        await _walletRepository.CreateTransactionAsync(
            new WalletTransaction
            {
                WalletID = wallet.WalletID,
                Amount = -booking.FinalAmount,
                BalanceAfter = wallet.Balance,
                TransactionType = WalletTransactionType.Payment,
                BookingID = bookingId,
                Description = $"Payment for booking {booking.BookingCode}",
                CreatedAt = DateTime.Now
            },
            cancellationToken);

        await _bookingRepository.UpdateBookingStatusAsync(
            bookingId,
            BookingStatus.Paid,
            cancellationToken);

        var invoice = await _invoiceService.CreateInvoiceAsync(bookingId, cancellationToken);
        var pointsEarned = CalculatePointsEarned(booking.FinalAmount);

        return new WalletPaymentResponse(
            Success: true,
            PaymentId: payment.PaymentID,
            BookingId: bookingId,
            PaymentMethod: PaymentMethod.Wallet,
            Amount: booking.FinalAmount,
            Status: PaymentStatus.Completed,
            BookingStatus: BookingStatus.Paid,
            InvoiceCode: invoice.InvoiceCode,
            PointsEarned: pointsEarned
        );
    }

    private async Task<VNPayPaymentResponse> ProcessVNPayPaymentAsync(
        int bookingId,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking is null)
            throw new InvalidOperationException($"Booking {bookingId} not found");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Booking {bookingId} is not pending");

        var existingPayment = await _paymentRepository.GetPaymentByBookingIdAsync(bookingId, cancellationToken);
        if (existingPayment is not null)
            throw new InvalidOperationException($"Payment already exists for booking {bookingId}");

        var payment = await _paymentRepository.CreatePaymentAsync(
            new Payment
            {
                BookingID = bookingId,
                PaymentMethod = PaymentMethod.VNPay,
                Amount = booking.FinalAmount,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.Now
            },
            cancellationToken);

        var expiresAt = DateTime.Now.AddMinutes(VNPaySessionExpiryMinutes);
        var qrCodeUrl = await _vnpayService.CreatePaymentUrlAsync(payment, booking, ipAddress, cancellationToken);

        var session = await _paymentRepository.CreatePaymentSessionAsync(
            new PaymentSession
            {
                PaymentID = payment.PaymentID,
                GatewayName = "VNPay",
                QRCodeURL = qrCodeUrl,
                ExpiresAt = expiresAt,
                Status = "waiting",
                CreatedAt = DateTime.Now
            },
            cancellationToken);

        return new VNPayPaymentResponse(
            Success: true,
            PaymentId: payment.PaymentID,
            BookingId: bookingId,
            PaymentMethod: PaymentMethod.VNPay,
            Amount: booking.FinalAmount,
            Status: PaymentStatus.Pending,
            QrCodeUrl: qrCodeUrl,
            SessionId: session.SessionID,
            ExpiresAt: expiresAt
        );
    }

    private async Task<CashPaymentResponse> ProcessCashPaymentAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking is null)
            throw new InvalidOperationException($"Booking {bookingId} not found");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Booking {bookingId} is not pending");

        var existingPayment = await _paymentRepository.GetPaymentByBookingIdAsync(bookingId, cancellationToken);
        if (existingPayment is not null)
            throw new InvalidOperationException($"Payment already exists for booking {bookingId}");

        var payment = await _paymentRepository.CreatePaymentAsync(
            new Payment
            {
                BookingID = bookingId,
                PaymentMethod = PaymentMethod.Cash,
                Amount = booking.FinalAmount,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.Now
            },
            cancellationToken);

        return new CashPaymentResponse(
            Success: true,
            PaymentId: payment.PaymentID,
            BookingId: bookingId,
            PaymentMethod: PaymentMethod.Cash,
            Amount: booking.FinalAmount,
            Status: PaymentStatus.Pending,
            Message: "Please pay at the counter before the showtime"
        );
    }

    private static PaymentResponse? MapToPaymentResponse(Payment? payment)
    {
        if (payment is null)
            return null;

        return new PaymentResponse(
            PaymentId: payment.PaymentID,
            BookingId: payment.BookingID,
            PaymentMethod: payment.PaymentMethod,
            Amount: payment.Amount,
            Status: payment.Status,
            PaidAt: payment.PaidAt,
            CreatedAt: payment.CreatedAt
        );
    }

    private static int CalculatePointsEarned(decimal amount)
    {
        return (int)(amount / 1000m * PointsPerThousand);
    }

    private static int ExtractPaymentIdFromTxnRef(string txnRef)
    {
        var parts = txnRef.Split('_');
        if (parts.Length >= 2 && int.TryParse(parts[1], out var paymentId))
            return paymentId;

        return 0;
    }
}
