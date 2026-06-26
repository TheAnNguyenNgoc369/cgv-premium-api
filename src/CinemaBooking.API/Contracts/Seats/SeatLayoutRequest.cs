namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatLayoutRequest(
    int TotalRows,
    int TotalCols,
    IReadOnlyCollection<SeatLayoutSeatRequest> Seats
);
