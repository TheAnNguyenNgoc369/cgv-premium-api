namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatLayoutSeatRequest(
    string? RowLabel,
    int ColIndex,
    string? SeatName,
    int? SeatTypeId,
    string? Status,
    bool IsWalkway
);
