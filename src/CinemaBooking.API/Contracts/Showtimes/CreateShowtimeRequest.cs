namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record CreateShowtimeRequest(
    int MovieId,
    int RoomId,
    DateTime StartTime,
    decimal BasePrice);
