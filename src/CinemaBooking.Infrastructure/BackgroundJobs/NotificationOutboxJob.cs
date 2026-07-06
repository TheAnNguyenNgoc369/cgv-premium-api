using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
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
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IBookingEmailService>();
        var now = DateTime.UtcNow;
        var events = await db.NotificationOutbox.Where(x => x.Status != "processed" && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var item in events)
        {
            try
            {
                if (item.EventType == "BookingSuccess") await ProcessBookingAsync(db, email, item, ct);
                else if (item.EventType == "RefundCompleted") await ProcessRefundAsync(db, email, item, ct);
                item.Status = "processed"; item.ProcessedAt = DateTime.UtcNow; item.LastError = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                item.AttemptCount++; item.Status = item.AttemptCount >= 3 ? "failed" : "pending";
                item.NextAttemptAt = DateTime.UtcNow.AddMinutes(item.AttemptCount switch { 1 => 1, 2 => 5, _ => 15 });
                item.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                logger.LogError(ex, "Notification outbox event {EventId} failed", item.EventId);
            }
            await db.SaveChangesAsync(ct);
        }
    }
    private static async Task ProcessBookingAsync(CinemaBookingDbContext db, IBookingEmailService email, NotificationOutbox item, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().Include(x => x.User).FirstAsync(x => x.BookingID == item.ReferenceId, ct);
        await AddNotificationAsync(db, item, booking.UserID!.Value, "Đặt vé thành công", $"Đơn {booking.BookingCode} đã thanh toán thành công.", "booking", "Booking", $"/bookings/{booking.BookingID}", ct);
        await email.QueueBookingConfirmedAsync(booking.BookingID, ct);
    }
    private static async Task ProcessRefundAsync(CinemaBookingDbContext db, IBookingEmailService email, NotificationOutbox item, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().FirstAsync(x => x.BookingID == item.ReferenceId, ct);
        await AddNotificationAsync(db, item, booking.UserID!.Value, "Hoàn tiền thành công", $"Đơn {booking.BookingCode} đã được hoàn tiền.", "refund", "Refund", $"/bookings/{booking.BookingID}", ct);
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
