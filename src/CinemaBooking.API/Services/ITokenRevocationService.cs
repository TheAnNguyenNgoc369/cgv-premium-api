namespace CinemaBooking.API.Services;

public interface ITokenRevocationService
{
    Task RevokeAsync(
        string token,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(
        string token,
        CancellationToken cancellationToken = default);
}
