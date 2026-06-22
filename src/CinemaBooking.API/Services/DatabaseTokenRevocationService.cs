using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaBooking.Application.Common.Interfaces;

namespace CinemaBooking.API.Services;

public sealed class DatabaseTokenRevocationService : ITokenRevocationService
{
    private readonly IUserRepository _userRepository;

    public DatabaseTokenRevocationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task RevokeAsync(
        string token,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || expiresAtUtc <= DateTime.UtcNow)
        {
            return;
        }

        if (!TryReadAuthenticationState(token, out var userId, out var tokenVersion))
        {
            return;
        }

        await _userRepository.TryIncrementTokenVersionAsync(
            userId,
            tokenVersion,
            cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (!TryReadAuthenticationState(token, out var userId, out var tokenVersion))
        {
            return true;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        return user is null
            || !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase)
            || user.TokenVersion != tokenVersion;
    }

    private static bool TryReadAuthenticationState(
        string token,
        out int userId,
        out int tokenVersion)
    {
        userId = 0;
        tokenVersion = 0;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var userIdValue = jwtToken.Claims.FirstOrDefault(claim =>
                claim.Type is ClaimTypes.NameIdentifier or JwtRegisteredClaimNames.Sub)?.Value;
            var tokenVersionValue = jwtToken.Claims.FirstOrDefault(claim =>
                claim.Type == JwtTokenService.TokenVersionClaim)?.Value;

            return int.TryParse(userIdValue, out userId)
                && int.TryParse(tokenVersionValue, out tokenVersion);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
