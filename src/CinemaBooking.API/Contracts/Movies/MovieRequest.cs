namespace CinemaBooking.API.Contracts.Movies;

public sealed class MovieRequest
{
    public string Title { get; init; } = string.Empty;

    public List<string>? Genres { get; init; }

    public string? AgeRating { get; init; }

    public string Director { get; init; } = string.Empty;

    public string? Cast { get; init; }

    public string? Synopsis { get; init; }

    public int DurationMinutes { get; init; }

    public DateOnly? ShowingFromDate { get; init; }

    public DateOnly? ShowingToDate { get; init; }

    public string? PosterUrl { get; init; }

    public string? PosterPublicId { get; init; }

    public string? TrailerUrl { get; init; }

    public string? Status { get; init; }
}
