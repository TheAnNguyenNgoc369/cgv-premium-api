namespace CinemaBooking.Application.Payments.PayOS;

public interface IPayOSService
{
    Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int bookingId,
        int amount,
        string description,
        DateTime expiresAt,
        string? backendOrigin = null,
        CancellationToken cancellationToken = default);

    Task<PayOSVerifiedWebhookData> VerifyWebhookAsync(
        PayOSWebhook webhook,
        CancellationToken cancellationToken = default);

    Task<PayOSPaymentLinkStatusResult> GetPaymentLinkStatusAsync(
        long orderCode,
        CancellationToken cancellationToken = default);

    Task ConfirmConfiguredWebhookAsync(
        CancellationToken cancellationToken = default);
}
