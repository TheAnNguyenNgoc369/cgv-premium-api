namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatRequest(
    string RowLabel,
    int SeatNumber,
    int? SeatTypeId,
    string? Status,
    bool IsGap
);
