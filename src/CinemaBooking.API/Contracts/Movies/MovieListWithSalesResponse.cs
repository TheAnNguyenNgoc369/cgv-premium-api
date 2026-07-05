namespace CinemaBooking.API.Contracts.Movies;

public sealed record MovieListWithSalesResponse(
    int MovieId,
    string Title,
    List<string> Genres,
    string AgeRating,
    string? PosterUrl,
    int DurationMinutes,
    string Status,
    int TicketsSold,
    bool IsTopSelling,
    int? SalesRank);
