using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Seats;

public sealed record SeatLayoutResult(
    int TotalRows,
    int TotalCols,
    List<Seat> Seats
);
