using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Persons;

public sealed record PersonSummary(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);
