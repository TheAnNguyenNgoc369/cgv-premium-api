namespace CinemaBooking.Application.Seats;

public sealed record SeatLayoutSeatItem(
    string? RowLabel,
    int ColIndex,
    string? SeatName,
    int? SeatTypeId,
    string? Status,
    bool IsGap
);
