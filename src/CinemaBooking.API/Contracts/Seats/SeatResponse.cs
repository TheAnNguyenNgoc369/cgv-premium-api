namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatResponse(
    int SeatId,
    int RoomId,
    string RowLabel,
    int SeatNumber,
    string SeatCode,
    string Type,
    string Status
);
