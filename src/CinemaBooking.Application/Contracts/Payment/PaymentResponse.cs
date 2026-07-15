namespace CinemaBooking.Application.Contracts.Payment;

public sealed record PaymentResponse(
    int PaymentId,
    int BookingId,
    string PaymentMethod,
    decimal Amount,
    string Status,
    DateTime? PaidAt,
    DateTime CreatedAt
);

public sealed record WalletPaymentResponse(
    bool Success,
    int PaymentId,
    int BookingId,
    string PaymentMethod,
    decimal Amount,
    string Status,
    string BookingStatus,
    string? InvoiceCode,
    int PointsEarned
);

public sealed record CashPaymentResponse(
    bool Success,
    int PaymentId,
    int BookingId,
    string PaymentMethod,
    decimal Amount,
    string Status,
    string Message
);

public sealed record InitiatePaymentResponse(
    bool Success,
    object Payment,
    PaymentBookingResponse Booking,
    int PaymentId,
    int BookingId,
    string PaymentMethod,
    decimal Amount,
    string Status,
    string? CheckoutUrl = null,
    string? QrCode = null,
    string? PaymentLinkId = null,
    long? OrderCode = null,
    int? SessionId = null,
    DateTime? ExpiresAt = null
);

public sealed record PaymentBookingResponse(
    int BookingId,
    string BookingCode,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal FinalAmount,
    string Status,
    DateTime BookingDate
);
