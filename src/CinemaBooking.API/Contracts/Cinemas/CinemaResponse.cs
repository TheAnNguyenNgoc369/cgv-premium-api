namespace CinemaBooking.API.Contracts.Cinemas;

public sealed record CinemaResponse(
    int CinemaID,
    string CinemaName,
    string Address
);