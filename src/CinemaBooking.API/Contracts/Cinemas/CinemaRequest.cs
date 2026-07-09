using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Cinemas;

public sealed record CinemaRequest(
    string CinemaName,
    string Address,
    string? Status,
    [param: Range(-90d, 90d)] double? Latitude,
    [param: Range(-180d, 180d)] double? Longitude
);
