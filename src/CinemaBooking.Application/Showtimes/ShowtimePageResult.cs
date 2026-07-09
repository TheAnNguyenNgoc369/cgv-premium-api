using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Showtimes;

public sealed record ShowtimePageResult(
    IReadOnlyList<Showtime> Items,
    IReadOnlySet<int> SoldOutShowtimeIds,
    int Page,
    int PageSize,
    int TotalItems);
