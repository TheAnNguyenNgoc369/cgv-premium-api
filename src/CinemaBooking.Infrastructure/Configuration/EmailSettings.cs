using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Infrastructure.Configuration;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    [Required]
    public string FromName { get; init; } = string.Empty;

    public string? Username { get; init; }

    public string? Password { get; init; }
}
