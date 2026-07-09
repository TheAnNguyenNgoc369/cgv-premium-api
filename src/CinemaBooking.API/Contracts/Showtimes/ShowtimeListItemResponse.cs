using CinemaBooking.API.Contracts.Cinemas;

namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeListItemResponse(
    int ShowtimeId,
    ShowtimeMovieResponse Movie,
    ShowtimeRoomResponse Room,
    CinemaSummaryResponse Cinema,
    DateTime StartTime,
    DateTime EndTime,
    decimal BasePrice,
    string Status,
    bool IsActive,
    bool IsSoldOut);
