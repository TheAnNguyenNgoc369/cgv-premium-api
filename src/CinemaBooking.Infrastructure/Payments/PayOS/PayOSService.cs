using CinemaBooking.Application.Payments.PayOS;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace CinemaBooking.Infrastructure.Payments.PayOS;

public sealed class PayOSService : IPayOSService
{
    private readonly PayOSSettings _settings;

    public PayOSService(IOptions<PayOSSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            ReturnUrl = _settings.ReturnUrl,
            CancelUrl = _settings.CancelUrl,
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

    private PayOSClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            throw new InvalidOperationException(
                "PayOS credentials are not configured. Set PayOS__ClientId, PayOS__ApiKey and PayOS__ChecksumKey.");

        return new PayOSClient(_settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);
    }
}
