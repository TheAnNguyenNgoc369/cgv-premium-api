namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatUpdateRequest(
    string Type,
    string Status
);
