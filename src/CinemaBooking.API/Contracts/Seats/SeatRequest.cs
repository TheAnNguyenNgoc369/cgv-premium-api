namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatRequest(
    string RowLabel,
    int SeatNumber,
    string? SeatCode,
    string Type,
    string Status
);
