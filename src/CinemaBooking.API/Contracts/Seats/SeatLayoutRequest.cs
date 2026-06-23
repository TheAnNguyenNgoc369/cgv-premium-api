namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatLayoutRequest(
    int TotalRows,
    int SeatsPerRow,
    string SeatType,
    string SeatStatus
);
