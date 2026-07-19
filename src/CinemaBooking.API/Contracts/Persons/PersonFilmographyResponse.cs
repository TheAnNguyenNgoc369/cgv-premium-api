using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Persons;

public sealed record PersonFilmographyResponse(
    [property: JsonPropertyName("personId")] int PersonId,
    [property: JsonPropertyName("personName")] string PersonName,
    [property: JsonPropertyName("totalMovies")] int TotalMovies,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("items")] IReadOnlyList<FilmographyItem> Items);
