using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Persons;

public sealed record PagedPersonAutocompleteResponse(
    [property: JsonPropertyName("items")] IReadOnlyList<PersonAutocompleteItem> Items,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("total")] int Total);
