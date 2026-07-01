using CinemaBooking.API.Validation;

namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record CreateShowtimeRequest(
    int MovieId,
    int RoomId,
    [property: VietnamDateTimeOffset] DateTimeOffset StartTime,
    decimal BasePrice);
