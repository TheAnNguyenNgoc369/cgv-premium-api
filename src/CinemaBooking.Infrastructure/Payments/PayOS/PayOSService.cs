using CinemaBooking.Application.Payments.PayOS;
using CinemaBooking.Application.Configuration;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace CinemaBooking.Infrastructure.Payments.PayOS;

public sealed class PayOSService : IPayOSService
{
    private const string DefaultReturnPath = "payment/result";
    private const string DefaultCancelPath = "payment/cancel";

    private readonly PayOSSettings _settings;
    private readonly FrontendSettings _frontendSettings;

    public PayOSService(IOptions<PayOSSettings> settings, IOptions<FrontendSettings> frontendSettings)
    {
        _settings = settings.Value;
        _frontendSettings = frontendSettings.Value;
    }

    public async Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int bookingId,
        int amount,
        string description,
        DateTime expiresAt,
        string? backendOrigin = null,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var returnUrl = ResolveRedirectUrl(_settings.ReturnUrl, DefaultReturnPath, backendOrigin);
        var cancelUrl = ResolveRedirectUrl(_settings.CancelUrl, DefaultCancelPath, backendOrigin);
        var response = await client.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            ReturnUrl = BuildRedirectUrl(returnUrl, bookingId, orderCode),
            CancelUrl = BuildRedirectUrl(cancelUrl, bookingId, orderCode),
            ExpiredAt = new DateTimeOffset(expiresAt).ToUnixTimeSeconds()
        });

        return new PayOSPaymentLinkResult(
            response.OrderCode,
            response.PaymentLinkId,
            response.CheckoutUrl,
            response.QrCode);
    }

    public async Task<PayOSVerifiedWebhookData> VerifyWebhookAsync(
        PayOSWebhook webhook,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var verified = await client.Webhooks.VerifyAsync(new Webhook
        {
            Code = webhook.Code,
            Description = webhook.Description,
            Success = webhook.Success,
            Data = new WebhookData
            {
                OrderCode = webhook.Data.OrderCode,
                Amount = webhook.Data.Amount,
                Description = webhook.Data.Description,
                AccountNumber = webhook.Data.AccountNumber,
                Reference = webhook.Data.Reference,
                TransactionDateTime = webhook.Data.TransactionDateTime,
                Currency = webhook.Data.Currency,
                PaymentLinkId = webhook.Data.PaymentLinkId,
                Code = webhook.Data.Code,
                Description2 = webhook.Data.DescriptionDetail,
                CounterAccountBankId = webhook.Data.CounterAccountBankId ?? string.Empty,
                CounterAccountBankName = webhook.Data.CounterAccountBankName ?? string.Empty,
                CounterAccountName = webhook.Data.CounterAccountName ?? string.Empty,
                CounterAccountNumber = webhook.Data.CounterAccountNumber ?? string.Empty,
                VirtualAccountName = webhook.Data.VirtualAccountName ?? string.Empty,
                VirtualAccountNumber = webhook.Data.VirtualAccountNumber ?? string.Empty
            },
            Signature = webhook.Signature
        });

        return new PayOSVerifiedWebhookData(
            verified.OrderCode,
            checked((int)verified.Amount),
            verified.Reference,
            verified.PaymentLinkId,
            verified.Code);
    }

    public async Task<PayOSPaymentLinkStatusResult> GetPaymentLinkStatusAsync(
        long orderCode,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var paymentLink = await client.PaymentRequests.GetAsync(orderCode);

        return new PayOSPaymentLinkStatusResult(paymentLink.Status.ToString());
    }

    public async Task ConfirmConfiguredWebhookAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookUrl))
            return;

        var client = CreateClient();
        await client.Webhooks.ConfirmAsync(_settings.WebhookUrl);
    }

    private PayOSClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            throw new InvalidOperationException(
                "PayOS credentials are not configured. Set PayOS__ClientId, PayOS__ApiKey and PayOS__ChecksumKey.");

        return new PayOSClient(_settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);
    }

    internal string ResolveRedirectUrl(string configuredUrl, string frontendPath, string? backendOrigin = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredUrl))
            return configuredUrl;

        var baseUrl = IsPublicHttpsUrl(backendOrigin)
            ? backendOrigin!
            : _frontendSettings.BaseUrl;

        return $"{baseUrl.TrimEnd('/')}/{frontendPath.TrimStart('/')}";
    }

    private static bool IsPublicHttpsUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && !uri.IsLoopback;
    }

    internal static string BuildRedirectUrl(string baseUrl, int bookingId, long orderCode)
    {
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{baseUrl}{separator}bookingId={bookingId}&orderCode={orderCode}";
    }
}
