namespace CinemaBooking.API.Contracts.Cinemas;

public sealed record CinemaResponse(
    int CinemaId,
    string CinemaName,
    string Address,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
