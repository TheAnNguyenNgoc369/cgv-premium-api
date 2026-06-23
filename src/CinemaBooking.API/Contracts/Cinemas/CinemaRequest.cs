namespace CinemaBooking.API.Contracts.Cinemas;

public sealed record CinemaRequest(
    string CinemaName,
    string Address,
    string? Status
);
