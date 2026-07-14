using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Application.Payments.PayOS;

public sealed class PayOSSettings
{
    public const string SectionName = "PayOS";

    public string ClientId { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ChecksumKey { get; init; } = string.Empty;

    public string ReturnUrl { get; init; } = string.Empty;

    public string CancelUrl { get; init; } = string.Empty;

    [Required, Url]
    public string WebhookUrl { get; init; } = string.Empty;
}
