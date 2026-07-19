namespace CinemaBooking.Application.Features.AI.DTOs;

public sealed record IntentResult
{
    public string Intent { get; init; } = "general";
    public decimal Confidence { get; init; }
    public string? ExtractedGenre { get; init; }
    public string? ExtractedKeyword { get; init; }
}
