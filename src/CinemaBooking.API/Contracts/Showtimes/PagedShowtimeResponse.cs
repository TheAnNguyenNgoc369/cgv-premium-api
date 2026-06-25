namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record PagedShowtimeResponse(
    IReadOnlyList<ShowtimeManagementResponse> Items,
    int Page, int PageSize, int TotalItems, int TotalPages);
