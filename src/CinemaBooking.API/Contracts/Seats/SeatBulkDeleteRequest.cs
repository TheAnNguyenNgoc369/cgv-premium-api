namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatBulkDeleteRequest(
    SeatSelector Selector
);
