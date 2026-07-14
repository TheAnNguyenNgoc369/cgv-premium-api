namespace CinemaBooking.Application.Contracts.Payment;

public sealed record PayOSRedirectResult(
    bool Success,
    string? RedirectUrl,
    string Message);
