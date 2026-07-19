namespace CinemaBooking.Application.Features.AI.DTOs;

public sealed record MovieRecommendation
{
    public int MovieId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AgeRating { get; init; } = string.Empty;
    public int DurationMin { get; init; }
    public string? Description { get; init; }
    public DateOnly? ShowingFrom { get; init; }
    public DateOnly? ShowingTo { get; init; }
    public IReadOnlyList<string> Genres { get; init; } = [];
    public IReadOnlyList<string> Actors { get; init; } = [];
    public IReadOnlyList<string> Directors { get; init; } = [];
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}
