namespace CinemaBooking.Application.Showtimes;

public sealed record SeatMapSeatResult(
    int SeatID,
    string SeatRow,
    int SeatCol,
    string? SeatType,
    decimal ExtraPrice,
    decimal Price,
    string Status,
    bool IsGap);
