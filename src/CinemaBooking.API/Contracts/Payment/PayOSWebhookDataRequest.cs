using System.Text.Json.Serialization;
using CinemaBooking.Application.Payments.PayOS;

namespace CinemaBooking.API.Contracts.Payment;

public sealed class PayOSWebhookDataRequest
{
    public long OrderCode { get; init; }
    public int Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string TransactionDateTime { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string PaymentLinkId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("desc")]
    public string DescriptionDetail { get; init; } = string.Empty;

    public string? CounterAccountBankId { get; init; }
    public string? CounterAccountBankName { get; init; }
    public string? CounterAccountName { get; init; }
    public string? CounterAccountNumber { get; init; }
    public string? VirtualAccountName { get; init; }
    public string? VirtualAccountNumber { get; init; }

    public PayOSWebhookData ToApplicationModel() => new(
        OrderCode,
        Amount,
        Description,
        AccountNumber,
        Reference,
        TransactionDateTime,
        Currency,
        PaymentLinkId,
        Code,
        DescriptionDetail,
        CounterAccountBankId,
        CounterAccountBankName,
        CounterAccountName,
        CounterAccountNumber,
        VirtualAccountName,
        VirtualAccountNumber);
}
