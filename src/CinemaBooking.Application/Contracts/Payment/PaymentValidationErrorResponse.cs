namespace CinemaBooking.Application.Contracts.Payment;

public sealed record PaymentValidationErrorResponse(
    bool Success,
    string Message);
