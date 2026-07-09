using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CinemaBooking.Application.Payments.PayOS;

namespace CinemaBooking.API.Contracts.Payment;

public sealed class PayOSWebhookRequest
{
    [Required]
    public string Code { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("desc")]
    public string Description { get; init; } = string.Empty;

    public bool Success { get; init; }

    [Required]
    public PayOSWebhookDataRequest Data { get; init; } = new();

    [Required]
    public string Signature { get; init; } = string.Empty;

    public PayOSWebhook ToApplicationModel() => new(
        Code,
        Description,
        Success,
        Data.ToApplicationModel(),
        Signature);
}
