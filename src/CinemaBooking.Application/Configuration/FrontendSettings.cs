using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Application.Configuration;

public sealed class FrontendSettings
{
    public const string SectionName = "Frontend";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    public string[] AllowedOrigins { get; init; } = [];
}
