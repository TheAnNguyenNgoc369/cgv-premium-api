namespace CinemaBooking.API.Contracts.Movies;

public sealed record MovieListResponse(
    int MovieId,
    string Title,
    List<string> Genres,
    string AgeRating,
    string? PosterUrl,
    int DurationMinutes,
    string Status
);

public sealed record MovieDetailResponse(
    int MovieId,
    string Title,
    List<string> Genres,
    string AgeRating,
    string? Director,
    string? Cast,
    string? Synopsis,
    int DurationMinutes,
    DateOnly? ShowingFromDate,
    DateOnly? ShowingToDate,
    string? PosterUrl,
    string? PosterPublicId,
    string? TrailerUrl,
    string Status
);
