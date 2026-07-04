namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatBulkUpdate(
    int? SeatTypeId,
    string? Status,
    bool? IsGap
);
