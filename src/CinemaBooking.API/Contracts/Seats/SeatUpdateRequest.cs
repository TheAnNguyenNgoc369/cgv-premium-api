namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatUpdateRequest(
    int SeatTypeId,
    string Status
);
