namespace CinemaBooking.Application.Showtimes;

public sealed record SeatMapResult(
    int ShowtimeID,
    string RoomName,
    string RoomType,
    List<SeatMapSeatResult> Seats);
