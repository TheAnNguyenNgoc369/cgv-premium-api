using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace CinemaBooking.API.Services;

public sealed class AuthRequestRateLimiter : IAuthRequestRateLimiter, IDisposable
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, FixedWindowRateLimiter> _limiters = new();

    public async ValueTask<bool> TryAcquireAsync(
        string action,
        string email,
        CancellationToken cancellationToken = default)
    {
        var key = $"{action}:{email.Trim().ToLowerInvariant()}";
        var limiter = _limiters.GetOrAdd(key, _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = GetPermitLimit(action),
            QueueLimit = 0,
            Window = Window
        }));

        using var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken);
        return lease.IsAcquired;
    }

    public void Dispose()
    {
        foreach (var limiter in _limiters.Values)
        {
            limiter.Dispose();
        }
    }

    private static int GetPermitLimit(string action)
    {
        return action switch
        {
            "login" => 5,
            "register" => 3,
            "forgot-password" => 3,
            "resend-verification-email" => 3,
            _ => 5
        };
    }
}
