namespace CinemaBooking.Application.Features.AI.DTOs;

public sealed record ChatResponse
{
    public string Reply { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public IReadOnlyList<string> FollowUpQuestions { get; init; } = [];
}
