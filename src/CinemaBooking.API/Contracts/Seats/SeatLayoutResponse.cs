namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatLayoutResponse(
    int RoomId,
    int TotalRows,
    int TotalCols,
    IReadOnlyCollection<SeatResponse> Seats
);
