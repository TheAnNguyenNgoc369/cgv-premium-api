namespace CinemaBooking.API.Contracts.Movies;

public sealed record MovieListResponse(
    int MovieID,
    string Title,
    string AgeRating,
    string? PosterURL,
    int DurationMin,
    string Status,
    List<string> Genres
);

public sealed record MovieDetailResponse(
    int MovieID,
    string Title,
    string AgeRating,
    string? Director,
    string? Cast,
    string? Description,
    string? PosterURL,
    string? TrailerURL,
    int DurationMin,
    DateOnly? ShowingFrom,
    DateOnly? ShowingTo,
    string Status,
    List<string> Genres
);