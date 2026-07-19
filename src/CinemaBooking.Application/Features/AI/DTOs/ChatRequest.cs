using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Application.Features.AI.DTOs;

public sealed record ChatRequest
{
    [Required(ErrorMessage = "message is required.")]
    [MaxLength(2000, ErrorMessage = "message must not exceed 2000 characters.")]
    public string Message { get; init; } = string.Empty;

    public string? SessionId { get; init; }
}
