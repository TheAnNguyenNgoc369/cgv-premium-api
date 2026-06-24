namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeManagementResponse(
    int ShowtimeId, ShowtimeMovieResponse Movie, ShowtimeRoomResponse Room,
    DateTime StartTime, DateTime EndTime, decimal BasePrice, string Status, bool IsSoldOut);
