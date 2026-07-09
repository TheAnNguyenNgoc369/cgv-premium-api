namespace CinemaBooking.API.Contracts.Payment;

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
