namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeDetailResponse(
    int ShowtimeID, DateTime StartTime, DateTime EndTime, decimal BasePrice, string Status,
    int MovieID, string MovieTitle, string? PosterURL, int DurationMin, string AgeRating,
    int RoomID, string RoomName, string RoomType, int CinemaID, string CinemaName,
    string CinemaAddress);
