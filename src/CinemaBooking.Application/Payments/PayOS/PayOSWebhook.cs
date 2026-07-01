namespace CinemaBooking.Application.Payments.PayOS;

public sealed record PayOSWebhook(
    string Code,
    string Description,
    bool Success,
    PayOSWebhookData Data,
    string Signature);
