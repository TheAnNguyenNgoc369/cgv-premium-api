namespace CinemaBooking.Application.Seats;

public sealed record SeatSelector(
    string Mode,
    IReadOnlyCollection<string> Target
);
