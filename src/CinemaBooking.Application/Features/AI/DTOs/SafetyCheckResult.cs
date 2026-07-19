namespace CinemaBooking.Application.Features.AI.DTOs;

public sealed record SafetyCheckResult
{
    public bool IsSafe { get; init; }
    public string? Reason { get; init; }
    public string? SuggestedResponse { get; init; }
}
