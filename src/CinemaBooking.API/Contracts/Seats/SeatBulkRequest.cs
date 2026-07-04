namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatBulkRequest(
    SeatSelector Selector,
    SeatBulkUpdate Update
);
