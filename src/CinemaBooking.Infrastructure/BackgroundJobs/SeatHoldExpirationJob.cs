using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class SeatHoldExpirationJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatHoldExpirationJob> _logger;

    public SeatHoldExpirationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SeatHoldExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ExpireHoldsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to expire seat holds");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireHoldsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var expiredCount = await dbContext.SeatHolds
            .Where(hold => hold.Status == SeatHoldStatus.Holding
                && hold.ExpiresAt <= DateTime.UtcNow)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    hold => hold.Status,
                    SeatHoldStatus.Expired),
                cancellationToken);

        if (expiredCount > 0)
            _logger.LogInformation("Expired {SeatHoldCount} seat holds", expiredCount);
    }
}
