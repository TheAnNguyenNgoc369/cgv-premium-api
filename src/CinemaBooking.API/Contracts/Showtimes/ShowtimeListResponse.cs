namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeListResponse(
    int ShowtimeID, DateTime StartTime, DateTime EndTime, decimal BasePrice,
    int RoomID, string RoomName, string RoomType, int CinemaID, string CinemaName);
