using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBooking.API.Services;

public sealed class InMemoryTokenRevocationService : ITokenRevocationService
{
    private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

    public void Revoke(string token, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var normalizedExpiration = expiresAtUtc.Kind == DateTimeKind.Utc
            ? expiresAtUtc
            : expiresAtUtc.ToUniversalTime();

        if (normalizedExpiration <= DateTime.UtcNow)
        {
            return;
        }

        _revokedTokens[HashToken(token)] = normalizedExpiration;
    }

    public bool IsRevoked(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenHash = HashToken(token);

        if (!_revokedTokens.TryGetValue(tokenHash, out var expiresAtUtc))
        {
            return false;
        }

        if (expiresAtUtc > DateTime.UtcNow)
        {
            return true;
        }

        _revokedTokens.TryRemove(tokenHash, out _);
        return false;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}
