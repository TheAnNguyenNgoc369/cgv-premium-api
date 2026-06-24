namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeRequest(
    int MovieId,
    int RoomId,
    DateTime StartTime,
    decimal BasePrice,
    string Status);
