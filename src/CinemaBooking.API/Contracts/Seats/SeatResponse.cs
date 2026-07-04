namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatResponse(
    int SeatId,
    int RoomId,
    string RowLabel,
    int SeatNumber,
    string SeatCode,
    int? SeatTypeId,
    string? Type,
    string Status,
    bool IsGap
);
