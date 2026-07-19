using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class MovieAnalyticsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MovieAnalyticsJob> _logger;

    // Configurable thresholds
    private const double LowOccupancyThreshold = 0.3; // 30%
    private const int LowPerformanceDays = 5;
    private const int EndingSoonDays = 7;

    public MovieAnalyticsJob(IServiceScopeFactory scopeFactory, ILogger<MovieAnalyticsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once on startup
        await RunAnalyticsAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunAnalyticsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to run movie analytics");
            }
        }
    }

    private async Task RunAnalyticsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStart = today.AddDays(-LowPerformanceDays);

        // 1. Top Selling Movie (weekly)
        await CheckTopSellingMovieAsync(db, notificationOutbox, periodStart, today, ct);

        // 2. Low Performing Movies
        await CheckLowPerformingMoviesAsync(db, notificationOutbox, periodStart, today, ct);

        // 3. Movies Ending Soon
        await CheckMoviesEndingSoonAsync(db, notificationOutbox, today, ct);
    }

    private async Task CheckTopSellingMovieAsync(
        CinemaBookingDbContext db,
        INotificationOutbox notificationOutbox,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct)
    {
        var topMovie = await db.Bookings
            .Where(b => b.Status == "paid"
                     && b.BookingDate >= periodStart.ToDateTime(TimeOnly.MinValue)
                     && b.BookingDate <= periodEnd.ToDateTime(TimeOnly.MaxValue)
                     && b.ShowtimeID.HasValue)
            .GroupBy(b => b.Showtime!.MovieID)
            .Select(g => new 
            { 
                MovieId = g.Key, 
                Revenue = g.Sum(b => b.FinalAmount),
                Tickets = g.Sum(b => b.BookingSeats.Count)
            })
            .OrderByDescending(x => x.Revenue)
            .FirstOrDefaultAsync(ct);

        if (topMovie is null) return;

        // Check if same movie was top last week
        var lastPeriodStart = periodStart.AddDays(-LowPerformanceDays);
        var lastTopMovie = await db.Bookings
            .Where(b => b.Status == "paid"
                     && b.BookingDate >= lastPeriodStart.ToDateTime(TimeOnly.MinValue)
                     && b.BookingDate < periodStart.ToDateTime(TimeOnly.MinValue)
                     && b.ShowtimeID.HasValue)
            .GroupBy(b => b.Showtime!.MovieID)
            .Select(g => new { MovieId = g.Key, Revenue = g.Sum(b => b.FinalAmount) })
            .OrderByDescending(x => x.Revenue)
            .FirstOrDefaultAsync(ct);

        // Only notify if different movie is now top, or if it's the first time
        if (lastTopMovie is null || lastTopMovie.MovieId != topMovie.MovieId)
        {
            var movie = await db.Movie.FindAsync([topMovie.MovieId], ct);
            var message = $"{movie?.Title ?? "Movie"} is now the best-selling movie this week " +
                          $"with {topMovie.Revenue:N0} VND revenue and {topMovie.Tickets} tickets sold.";

            await notificationOutbox.EnqueueTopSellingMovieAsync(topMovie.MovieId, message, ct);
        }
    }

    private async Task CheckLowPerformingMoviesAsync(
        CinemaBookingDbContext db,
        INotificationOutbox notificationOutbox,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct)
    {
        var lowPerforming = await db.Movie
            .Where(m => m.Status == "showing"
                     && m.ShowingTo.HasValue
                     && m.ShowingTo.Value >= periodStart)
            .Select(m => new
            {
                m.MovieID,
                m.Title,
                TotalRevenue = m.Showtimes
                    .Where(s => s.Status == "scheduled" || s.Status == "completed")
                    .SelectMany(s => s.Bookings)
                    .Where(b => b.Status == "paid" 
                             && b.BookingDate >= periodStart.ToDateTime(TimeOnly.MinValue)
                             && b.BookingDate <= periodEnd.ToDateTime(TimeOnly.MaxValue))
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                TotalTickets = m.Showtimes
                    .Where(s => s.Status == "scheduled" || s.Status == "completed")
                    .SelectMany(s => s.Bookings)
                    .Where(b => b.Status == "paid"
                             && b.BookingDate >= periodStart.ToDateTime(TimeOnly.MinValue)
                             && b.BookingDate <= periodEnd.ToDateTime(TimeOnly.MaxValue))
                    .Sum(b => b.BookingSeats.Count),
                TotalShowtimes = m.Showtimes.Count(s => s.Status == "scheduled" || s.Status == "completed"),
                TotalCapacity = m.Showtimes
                    .Where(s => s.Status == "scheduled" || s.Status == "completed")
                    .SelectMany(s => s.Room!.Seats)
                    .Count(seat => seat.Status == "active" && !seat.IsGap)
            })
            .ToListAsync(ct);

        foreach (var movie in lowPerforming)
        {
            if (movie.TotalShowtimes == 0 || movie.TotalCapacity == 0) continue;

            var occupancyRate = (double)movie.TotalTickets / movie.TotalCapacity;

            if (occupancyRate < LowOccupancyThreshold)
            {
                var eventId = $"LowPerformingMovie:{movie.MovieID}:{periodEnd:yyyyMMdd}";
                var alreadySent = await db.NotificationOutbox
                    .AnyAsync(x => x.EventId == eventId && x.Status == "processed", ct);

                if (alreadySent) continue;

                var message = $"{movie.Title} has a low occupancy rate ({occupancyRate:P0}) " +
                             $"over the past {LowPerformanceDays} days. " +
                             $"Revenue: {movie.TotalRevenue:N0} VND, Tickets: {movie.TotalTickets}.";

                await notificationOutbox.EnqueueLowPerformingMovieAsync(movie.MovieID, message, ct);
            }
        }
    }

    private async Task CheckMoviesEndingSoonAsync(
        CinemaBookingDbContext db,
        INotificationOutbox notificationOutbox,
        DateOnly today,
        CancellationToken ct)
    {
        var endingSoon = await db.Movie
            .Where(m => m.Status == "showing"
                     && m.ShowingTo.HasValue
                     && m.ShowingTo.Value >= today
                     && m.ShowingTo.Value <= today.AddDays(EndingSoonDays))
            .Select(m => new { m.MovieID, m.Title, DaysLeft = m.ShowingTo!.Value.DayNumber - today.DayNumber })
            .ToListAsync(ct);

        foreach (var movie in endingSoon)
        {
            var eventId = $"MovieEndingSoon:{movie.MovieID}:{today:yyyyMMdd}";
            var alreadySent = await db.NotificationOutbox
                .AnyAsync(x => x.EventId == eventId && x.Status == "processed", ct);

            if (alreadySent) continue;

            var message = $"{movie.Title} will end its screening schedule in {movie.DaysLeft} day{(movie.DaysLeft > 1 ? "s" : "")}.";
            
            await notificationOutbox.EnqueueMovieEndingSoonAsync(
                movie.MovieID,
                message,
                movie.DaysLeft,
                ct);
        }
    }
}

public sealed class MovieAnalyticsSettings
{
    public const string SectionName = "MovieAnalytics";
    
    /// <summary>Low occupancy threshold (0.0 - 1.0)</summary>
    public double LowOccupancyThreshold { get; set; } = 0.3;
    
    /// <summary>Days to look back for performance analysis</summary>
    public int LowPerformanceDays { get; set; } = 5;
    
    /// <summary>Days before movie end date to send reminder</summary>
    public int EndingSoonDays { get; set; } = 7;
}