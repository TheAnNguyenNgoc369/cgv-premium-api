using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Movies;

public sealed record MovieListWithSalesResponse(
    int MovieId,
    string Title,
    List<string> Genres,
    string AgeRating,
    string? PosterUrl,
    int DurationMinutes,
    string Status,
    [property: JsonPropertyName("is_new")] bool IsNew,
    int TicketsSold,
    bool IsTopSelling,
    int? SalesRank);
