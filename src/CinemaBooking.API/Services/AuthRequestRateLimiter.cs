using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace CinemaBooking.API.Services;

public sealed class AuthRequestRateLimiter : IAuthRequestRateLimiter, IDisposable
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, RateLimiterEntry> _limiters = new();

    public async ValueTask<bool> TryAcquireAsync(
        string action,
        string email,
        CancellationToken cancellationToken = default)
    {
        var key = $"{action}:{email.Trim().ToLowerInvariant()}";
        var entry = _limiters.GetOrAdd(key, _ =>
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = GetPermitLimit(action),
                QueueLimit = 0,
                Window = Window
            });
            return new RateLimiterEntry(limiter);
        });

        entry.LastAccessedUtc = DateTime.UtcNow;

        using var lease = await entry.Limiter.AcquireAsync(permitCount: 1, cancellationToken);
        return lease.IsAcquired;
    }

    public void Dispose()
    {
        foreach (var entry in _limiters.Values)
            entry.Limiter.Dispose();
    }

    internal void CleanupStaleEntries()
    {
        var cutoff = DateTime.UtcNow - StaleThreshold;
        var staleKeys = _limiters
            .Where(kvp => kvp.Value.LastAccessedUtc < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            if (_limiters.TryRemove(key, out var entry))
                entry.Limiter.Dispose();
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

    internal sealed class RateLimiterEntry
    {
        public FixedWindowRateLimiter Limiter { get; }
        public DateTime LastAccessedUtc { get; set; }

        public RateLimiterEntry(FixedWindowRateLimiter limiter)
        {
            Limiter = limiter;
            LastAccessedUtc = DateTime.UtcNow;
        }
    }
}
