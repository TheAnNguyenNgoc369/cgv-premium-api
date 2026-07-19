using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Shared.Configuration;

public sealed class GeminiSettings
{
    public const string SectionName = "Gemini";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = "gemini-3.5-flash";

    public List<GeminiProviderSettings> Providers { get; init; } = [];
}

public sealed class GeminiProviderSettings
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public List<string> Models { get; init; } = [];
}
