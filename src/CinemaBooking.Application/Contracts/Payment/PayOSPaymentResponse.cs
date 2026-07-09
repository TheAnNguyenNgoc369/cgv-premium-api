namespace CinemaBooking.Application.Contracts.Payment;

public sealed record PayOSPaymentResponse(
    bool Success,
    int PaymentId,
    int BookingId,
    string PaymentMethod,
    decimal Amount,
    string Status,
    string CheckoutUrl,
    string QrCode,
    string PaymentLinkId,
    long OrderCode,
    int SessionId,
    DateTime ExpiresAt);
