namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeRangeResponse(
    IReadOnlyList<ShowtimeListItemResponse> Items);
