using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatGenerateRequest(
    int Rows,
    [property: JsonPropertyName("column")] int Columns,
    int SeatTypeId,
    string Status
);
