using CinemaBooking.API.Validation;

namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record UpdateShowtimeRequest(
    int MovieId,
    int RoomId,
    [param: VietnamDateTimeOffset] DateTimeOffset StartTime,
    decimal BasePrice,
    string? Status = null);
