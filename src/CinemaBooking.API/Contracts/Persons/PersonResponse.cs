using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Persons;

public sealed record PersonResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("biography")] string? Biography,
    [property: JsonPropertyName("dateOfBirth")] DateOnly? DateOfBirth,
    [property: JsonPropertyName("nationality")] string? Nationality,
    [property: JsonPropertyName("gender")] string? Gender,
    [property: JsonPropertyName("photoUrl")] string? PhotoUrl,
    [property: JsonPropertyName("photoPublicId")] string? PhotoPublicId,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt
);
