namespace CinemaBooking.Application.Payments.PayOS;

public sealed record PayOSVerifiedWebhookData(
    long OrderCode,
    int Amount,
    string Reference,
    string PaymentLinkId,
    string Code);
