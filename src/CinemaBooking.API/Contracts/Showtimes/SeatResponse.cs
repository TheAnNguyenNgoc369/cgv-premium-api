namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record SeatResponse(
    int SeatID, string SeatRow, int SeatCol, string SeatType,
    decimal ExtraPrice, decimal Price, string Status);
