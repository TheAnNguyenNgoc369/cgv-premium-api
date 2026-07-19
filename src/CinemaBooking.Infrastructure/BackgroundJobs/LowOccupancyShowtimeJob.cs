using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class LowOccupancyShowtimeJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LowOccupancyShowtimeJob> _logger;

    // Configurable thresholds
    private const double OccupancyThreshold = 0.2; // 20%
    private const int LookAheadHours = 4; // Showtimes starting within 4 hours

    public LowOccupancyShowtimeJob(IServiceScopeFactory scopeFactory, ILogger<LowOccupancyShowtimeJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        do
        {
            try
            {
                await CheckLowOccupancyShowtimesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to check low occupancy showtimes");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckLowOccupancyShowtimesAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var now = DateTime.UtcNow;
        var windowEnd = now.AddHours(LookAheadHours);

        var showtimes = await db.Showtimes
            .Where(s => s.Status == "scheduled"
                     && s.StartTime > now
                     && s.StartTime <= windowEnd)
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .AsSplitQuery()
            .ToListAsync(ct);

        var showtimeIds = showtimes.Select(s => s.ShowtimeID).ToList();

        var bookedCounts = await db.SeatHolds
            .Where(h => showtimeIds.Contains(h.ShowtimeID) && h.Status == "confirmed")
            .GroupBy(h => h.ShowtimeID)
            .Select(g => new { ShowtimeID = g.Key, Count = g.Select(h => h.SeatID).Distinct().Count() })
            .ToDictionaryAsync(x => x.ShowtimeID, x => x.Count, ct);

        foreach (var showtime in showtimes)
        {
            var totalSeats = showtime.Room?.Capacity ?? 0;
            if (totalSeats == 0) continue;

            var bookedSeats = bookedCounts.GetValueOrDefault(showtime.ShowtimeID);

            var occupancyRate = (double)bookedSeats / totalSeats;

            if (occupancyRate < OccupancyThreshold)
            {
                // Check if already notified (eventId must match EnqueueLowOccupancyShowtimeAsync format)
                var eventId = $"LowOccupancyShowtime:{showtime.ShowtimeID}:{now:yyyyMMdd}";
                var alreadySent = await db.NotificationOutbox
                    .AnyAsync(x => x.EventId == eventId && x.Status == "processed", ct);
                
                if (alreadySent) continue;

                var message = $"Showtime {showtime.StartTime:HH:mm} - {showtime.Movie!.Title} " +
                              $"has low occupancy ({occupancyRate:P0}). " +
                              $"{bookedSeats}/{totalSeats} seats filled.";

                await notificationOutbox.EnqueueLowOccupancyShowtimeAsync(
                    showtime.ShowtimeID,
                    message,
                    ct);

                _logger.LogInformation("Low occupancy alert for showtime {ShowtimeId}: {Rate:P0}", 
                    showtime.ShowtimeID, occupancyRate);
            }
        }
    }
}

public sealed class LowOccupancySettings
{
    public const string SectionName = "LowOccupancy";
    
    /// <summary>Occupancy threshold below which to trigger alert (0.0 - 1.0)</summary>
    public double ThresholdPercent { get; set; } = 0.2;
    
    /// <summary>Look-ahead hours for upcoming showtimes</summary>
    public int LookAheadHours { get; set; } = 4;
}