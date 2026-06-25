namespace CinemaBooking.Application.Contracts.Payment;

public sealed record VNPayCallbackResult(
    bool Success,
    string Message,
    int? PaymentId = null,
    int? BookingId = null,
    string? PaymentStatus = null,
    string? BookingStatus = null
);
