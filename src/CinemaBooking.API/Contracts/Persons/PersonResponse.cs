using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Persons;

public sealed record PersonResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt
);
