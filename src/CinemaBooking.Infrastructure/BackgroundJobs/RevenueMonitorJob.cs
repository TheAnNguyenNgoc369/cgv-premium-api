using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class RevenueMonitorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RevenueMonitorJob> _logger;

    // Configurable threshold (25% by default)
    private const decimal ThresholdPercent = 25.0m;
    // Compare against same time yesterday
    private const int LookbackDays = 1;

    public RevenueMonitorJob(IServiceScopeFactory scopeFactory, ILogger<RevenueMonitorJob> logger)
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
                await CheckRevenueAnomaliesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to check revenue anomalies");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckRevenueAnomaliesAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var yesterdayStart = todayStart.AddDays(-LookbackDays);

        // Get all active cinemas
        var cinemas = await db.Cinemas
            .Where(c => c.Status == "active")
            .Select(c => new { c.CinemaID, c.CinemaName })
            .ToListAsync(ct);

        foreach (var cinema in cinemas)
        {
            var todayRevenue = await GetRevenueAsync(db, cinema.CinemaID, todayStart, now, ct);
            var yesterdayRevenue = await GetRevenueAsync(db, cinema.CinemaID, yesterdayStart, now.AddDays(-LookbackDays), ct);

            if (yesterdayRevenue > 0)
            {
                var changePercent = (todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100;
                
                if (Math.Abs(changePercent) >= ThresholdPercent)
                {
                    var direction = changePercent > 0 ? "increased" : "decreased";
                    var message = $"Revenue has {direction} by {Math.Abs(changePercent):F1}% compared to same time yesterday. " +
                                 $"Today: {todayRevenue:N0} VND, Yesterday: {yesterdayRevenue:N0} VND.";
                    
                    await notificationOutbox.EnqueueRevenueAnomalyAsync(cinema.CinemaID, message, ct);
                    _logger.LogInformation("Revenue anomaly detected for cinema {CinemaId}: {Change}%", cinema.CinemaID, changePercent);
                }
            }
        }

        // Global check for Admin
        var globalToday = await GetRevenueAsync(db, null, todayStart, now, ct);
        var globalYesterday = await GetRevenueAsync(db, null, yesterdayStart, now.AddDays(-LookbackDays), ct);

        if (globalYesterday > 0)
        {
            var changePercent = (globalToday - globalYesterday) / globalYesterday * 100;
            if (Math.Abs(changePercent) >= ThresholdPercent)
            {
                var direction = changePercent > 0 ? "increased" : "decreased";
                var message = $"Global revenue has {direction} by {Math.Abs(changePercent):F1}% compared to same time yesterday. " +
                             $"Today: {globalToday:N0} VND, Yesterday: {globalYesterday:N0} VND.";
                
                await notificationOutbox.EnqueueRevenueAnomalyAsync(0, message, ct); // 0 = global
                _logger.LogInformation("Global revenue anomaly detected: {Change}%", changePercent);
            }
        }
    }

    private static async Task<decimal> GetRevenueAsync(
        CinemaBookingDbContext db,
        int? cinemaId,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var query = db.Bookings
            .Where(b => b.Status == "paid"
                     && b.BookingDate >= from
                     && b.BookingDate <= to);

        if (cinemaId.HasValue)
        {
            query = query.Where(b => b.Showtime!.Room.CinemaID == cinemaId.Value);
        }

        return await query.SumAsync(b => (decimal?)b.FinalAmount, ct) ?? 0;
    }
}