namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record CustomerShowtimeResponse(
    int ShowtimeId,
    int MovieId,
    string MovieTitle,
    int CinemaId,
    string CinemaName,
    int RoomId,
    string RoomName,
    string RoomType,
    DateTime StartTime,
    DateTime EndTime,
    decimal BasePrice);
