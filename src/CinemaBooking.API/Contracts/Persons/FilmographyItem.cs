using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Persons;

public sealed record FilmographyItem(
    [property: JsonPropertyName("movieId")] int MovieId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("posterUrl")] string? PosterUrl,
    [property: JsonPropertyName("releaseDate")] DateOnly? ReleaseDate,
    [property: JsonPropertyName("duration")] int Duration,
    [property: JsonPropertyName("ageRating")] string AgeRating,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles);
