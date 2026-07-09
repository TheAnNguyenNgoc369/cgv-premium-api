namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record PagedShowtimeResponse(
    IReadOnlyList<ShowtimeListItemResponse> Items,
    int Page, int PageSize, int TotalItems, int TotalPages);
