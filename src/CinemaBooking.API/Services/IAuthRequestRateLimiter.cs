namespace CinemaBooking.API.Services;

public interface IAuthRequestRateLimiter
{
    ValueTask<bool> TryAcquireAsync(
        string action,
        string email,
        CancellationToken cancellationToken = default);
}
