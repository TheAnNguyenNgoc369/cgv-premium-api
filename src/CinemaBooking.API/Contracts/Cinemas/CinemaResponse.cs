namespace CinemaBooking.API.Contracts.Cinemas;

public sealed record CinemaResponse(
    int CinemaId,
    string CinemaName,
    string Address,
    double? Latitude,
    double? Longitude,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
