namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeMovieResponse(
    int MovieId, string Title, string AgeRating, int DurationMin, string? PosterUrl);
