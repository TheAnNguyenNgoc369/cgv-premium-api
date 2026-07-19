using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class DailySummaryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySummaryJob> _logger;

    public DailySummaryJob(IServiceScopeFactory scopeFactory, ILogger<DailySummaryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run at 00:05 daily
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddDays(1).AddMinutes(5);
        var delay = nextRun - now;

        await Task.Delay(delay, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                await GenerateDailySummariesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to generate daily summaries");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task GenerateDailySummariesAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var startOfDay = yesterday.ToDateTime(TimeOnly.MinValue);
        var endOfDay = yesterday.ToDateTime(TimeOnly.MaxValue);

        // Global summary for Admin
        var globalSummary = await db.Bookings
            .Where(b => b.Status == "paid"
                     && b.BookingDate >= startOfDay
                     && b.BookingDate <= endOfDay)
            .GroupBy(b => 1)
            .Select(g => new
            {
                Revenue = g.Sum(b => b.FinalAmount),
                Tickets = g.Sum(b => b.BookingSeats.Count),
                Showtimes = g.Select(b => b.ShowtimeID).Distinct().Count(),
            })
            .FirstOrDefaultAsync(ct);

        // Best movie separately (avoids nested GroupBy inside Select)
        var bestMovieName = await db.Bookings
            .Where(b => b.Status == "paid"
                     && b.BookingDate >= startOfDay
                     && b.BookingDate <= endOfDay
                     && b.ShowtimeID.HasValue)
            .GroupBy(b => b.Showtime!.Movie!.Title)
            .Select(g => new { Title = g.Key, Revenue = g.Sum(b => b.FinalAmount) })
            .OrderByDescending(x => x.Revenue)
            .Select(x => x.Title)
            .FirstOrDefaultAsync(ct);

        if (globalSummary is not null && globalSummary.Revenue > 0)
        {
            var message = $"Daily Summary for {yesterday:yyyy-MM-dd}: " +
                         $"Revenue: {globalSummary.Revenue:N0} VND | " +
                         $"Tickets: {globalSummary.Tickets} | " +
                         $"Showtimes: {globalSummary.Showtimes} | " +
                         $"Top Movie: {bestMovieName ?? "N/A"}";

            await notificationOutbox.EnqueueDailySummaryAsync(
                null, // null = global (Admin)
                message,
                ct);
        }

        // Per-cinema summaries for Managers
        var cinemaSummaries = await db.Bookings
            .Where(b => b.Status == "paid"
                     && b.BookingDate >= startOfDay
                     && b.BookingDate <= endOfDay
                     && b.ShowtimeID.HasValue)
            .GroupBy(b => b.Showtime!.Room!.CinemaID)
            .Select(g => new
            {
                CinemaId = g.Key,
                CinemaName = g.First().Showtime!.Room!.Cinema!.CinemaName,
                Revenue = g.Sum(b => b.FinalAmount),
                Tickets = g.Sum(b => b.BookingSeats.Count),
                Showtimes = g.Select(b => b.ShowtimeID).Distinct().Count(),
            })
            .ToListAsync(ct);

        // Fetch seat capacities per cinema separately
        var cinemaIds = cinemaSummaries.Select(s => s.CinemaId).ToList();
        var cinemaCapacities = await db.Rooms
            .Where(r => cinemaIds.Contains(r.CinemaID) && r.Status == "active")
            .SelectMany(r => r.Seats)
            .Where(s => s.Status == "active" && !s.IsGap)
            .GroupBy(s => s.Room.CinemaID)
            .Select(g => new { CinemaId = g.Key, TotalSeats = g.Count() })
            .ToDictionaryAsync(x => x.CinemaId, x => x.TotalSeats, ct);

        foreach (var summary in cinemaSummaries)
        {
            var totalSeats = cinemaCapacities.GetValueOrDefault(summary.CinemaId);
            var occupancyRate = totalSeats > 0 ? (double)summary.Tickets / totalSeats : 0;

            var message = $"Daily Summary for {summary.CinemaName} ({yesterday:yyyy-MM-dd}): " +
                         $"Revenue: {summary.Revenue:N0} VND | " +
                         $"Tickets: {summary.Tickets} | " +
                         $"Showtimes: {summary.Showtimes} | " +
                         $"Occupancy: {occupancyRate:P0}";

            await notificationOutbox.EnqueueDailySummaryAsync(
                summary.CinemaId,
                message,
                ct);
        }

        _logger.LogInformation("Daily summaries generated for {Count} cinemas + global", 
            cinemaSummaries.Count + (globalSummary?.Revenue > 0 ? 1 : 0));
    }
}