using System.Text.Json;

namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatSelector(
    string Mode,
    IReadOnlyCollection<JsonElement> Target
);
