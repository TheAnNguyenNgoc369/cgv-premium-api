namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record UpdateShowtimeRequest(
    int MovieId,
    int RoomId,
    DateTime StartTime,
    decimal BasePrice,
    string? Status = null);
