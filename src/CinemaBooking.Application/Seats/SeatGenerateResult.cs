using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Seats;

public sealed record SeatGenerateResult(
    int Rows,
    int Columns,
    List<Seat> Seats
);
