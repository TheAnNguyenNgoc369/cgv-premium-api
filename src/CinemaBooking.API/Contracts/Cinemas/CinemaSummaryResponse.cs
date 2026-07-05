namespace CinemaBooking.API.Contracts.Cinemas;

public sealed record CinemaSummaryResponse(
    int CinemaId,
    string CinemaName,
    string Address,
    double? Latitude,
    double? Longitude,
    string Status);
