using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Persons;

public sealed record PersonAutocompleteItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("photoUrl")] string? PhotoUrl,
    [property: JsonPropertyName("nationality")] string? Nationality);
