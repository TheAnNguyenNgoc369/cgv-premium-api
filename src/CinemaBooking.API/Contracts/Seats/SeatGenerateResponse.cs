namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatGenerateResponse(
    int RoomId,
    int Rows,
    int Columns,
    IReadOnlyCollection<SeatResponse> Seats
);
