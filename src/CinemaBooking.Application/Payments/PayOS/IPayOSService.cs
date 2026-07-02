namespace CinemaBooking.Application.Payments.PayOS;

public interface IPayOSService
{
    Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task<PayOSVerifiedWebhookData> VerifyWebhookAsync(
        PayOSWebhook webhook,
        CancellationToken cancellationToken = default);
}
