namespace CinemaBooking.Application.Bookings;

public sealed record SeatValidationErrors(
    IReadOnlyList<int> NotFoundSeatIds,
    IReadOnlyList<int> WrongRoomSeatIds,
    IReadOnlyList<int> InactiveSeatIds);
