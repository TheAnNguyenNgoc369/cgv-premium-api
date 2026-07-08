using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Email;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class NotificationOutboxJob(IServiceScopeFactory scopeFactory, ILogger<NotificationOutboxJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Notification outbox batch failed. The job will retry on the next interval.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IBookingEmailService>();
        var now = DateTime.UtcNow;
        var processingLeaseExpiresAt = now.AddMinutes(5);
        var candidateIds = await db.NotificationOutbox
            .Where(x =>
                (x.Status == "pending" || x.Status == "processing")
                && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.NotificationOutboxID)
            .Take(20)
            .ToListAsync(ct);

        var claimedIds = new List<long>();
        foreach (var eventId in candidateIds)
        {
            var affectedRows = await db.NotificationOutbox
                .Where(x => x.NotificationOutboxID == eventId
                    && (x.Status == "pending" || x.Status == "processing")
                    && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, "processing")
                        .SetProperty(x => x.NextAttemptAt, processingLeaseExpiresAt),
                    ct);

            if (affectedRows == 1)
                claimedIds.Add(eventId);
        }

        var events = await db.NotificationOutbox
            .Where(x => claimedIds.Contains(x.NotificationOutboxID))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
        foreach (var item in events)
        {
            try
            {
                if (item.EventType == "BookingSuccess") await ProcessBookingAsync(db, email, item, ct);
                else if (item.EventType == "RefundCompleted") await ProcessRefundAsync(db, email, item, ct);
                item.Status = "processed"; item.ProcessedAt = DateTime.UtcNow; item.LastError = null; item.NextAttemptAt = null;

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Notification outbox event {EventId} failed", item.EventId);

                try
                {
                    ScheduleRetry(item);
                    item.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception saveException) when (saveException is not OperationCanceledException)
                {
                    logger.LogError(
                        saveException,
                        "Failed to persist retry state for notification outbox event {EventId}",
                        item.EventId);
                }
            }
        }
    }

    private static void ScheduleRetry(NotificationOutbox item)
    {
        if (item.AttemptCount >= EmailRetryPolicy.MaxRetryCount)
        {
            item.Status = "failed";
            item.NextAttemptAt = null;
            return;
        }

        item.AttemptCount++;
        item.Status = "pending";
        item.NextAttemptAt = DateTime.UtcNow.Add(EmailRetryPolicy.GetDelay(item.AttemptCount));
    }

    private static async Task ProcessBookingAsync(CinemaBookingDbContext db, IBookingEmailService email, NotificationOutbox item, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().Include(x => x.User).FirstAsync(x => x.BookingID == item.ReferenceId, ct);
        await AddNotificationAsync(db, item, booking.UserID!.Value, "Booking successful", $"Order  {booking.BookingCode} has been successfully paid for.", "booking", "Booking", $"/bookings/{booking.BookingID}", ct);
        await email.QueueBookingConfirmedAsync(booking.BookingID, ct);
    }
    private static async Task ProcessRefundAsync(CinemaBookingDbContext db, IBookingEmailService email, NotificationOutbox item, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().FirstAsync(x => x.BookingID == item.ReferenceId, ct);
        await AddNotificationAsync(db, item, booking.UserID!.Value, "Refund successful", $"Order {booking.BookingCode} has been refunded.", "refund", "Refund", $"/bookings/{booking.BookingID}", ct);
        await email.QueueRefundProcessedAsync(booking.BookingID, item.Amount!.Value, item.OccurredAt!.Value, ct);
    }
    private static async Task AddNotificationAsync(CinemaBookingDbContext db, NotificationOutbox item, int userId, string title,
        string message, string type, string referenceType, string actionUrl, CancellationToken ct)
    {
        if (await db.Notifications.AnyAsync(x => x.EventId == item.EventId && x.UserID == userId, ct)) return;
        db.Notifications.Add(new Notification { UserID = userId, EventId = item.EventId, EventType = item.EventType,
            ReferenceType = referenceType, ReferenceId = item.ReferenceId, Title = title, Message = message, Type = type,
            ActionUrl = actionUrl, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }
}
