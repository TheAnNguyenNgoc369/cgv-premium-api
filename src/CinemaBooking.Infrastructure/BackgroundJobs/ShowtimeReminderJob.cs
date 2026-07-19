using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class ShowtimeReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShowtimeReminderJob> _logger;

    // Remind at 30 min and 15 min before
    private readonly int[] _reminderMinutes = { 30, 15 };

    public ShowtimeReminderJob(IServiceScopeFactory scopeFactory, ILogger<ShowtimeReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                await SendShowtimeRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send showtime reminders");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendShowtimeRemindersAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var now = DateTime.UtcNow;
        var windowEnd = now.AddMinutes(35); // Check for showtimes starting within 35 minutes

        var upcomingShowtimes = await db.Showtimes
            .Where(s => s.Status == "scheduled"
                     && s.StartTime > now
                     && s.StartTime <= windowEnd)
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
            .AsSplitQuery()
            .ToListAsync(ct);

        foreach (var showtime in upcomingShowtimes)
        {
            var minutesUntilStart = (int)(showtime.StartTime - now).TotalMinutes;

            foreach (var reminderMinutes in _reminderMinutes)
            {
                // Only notify if we're in the right window (within 2 minutes of target)
                if (minutesUntilStart <= reminderMinutes + 2 && minutesUntilStart >= reminderMinutes - 2)
                {
                    // Check if already sent for this reminder window
                    var eventId = $"ShowtimeStartingSoon:{showtime.ShowtimeID}:{reminderMinutes}m";
                    var alreadySent = await db.NotificationOutbox
                        .AnyAsync(x => x.EventId == eventId && x.Status == "processed", ct);
                    
                    if (alreadySent) continue;

                    var message = $"Showtime {showtime.StartTime:HH:mm} - {showtime.Movie!.Title} will begin in {reminderMinutes} minutes.";
                    
                    await notificationOutbox.EnqueueShowtimeStartingSoonAsync(
                        showtime.ShowtimeID,
                        message,
                        reminderMinutes,
                        ct);
                }
            }
        }
    }
}