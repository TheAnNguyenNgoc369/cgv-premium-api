using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Notifications;

public sealed class NotificationOutbox : INotificationOutbox
{
    private readonly CinemaBookingDbContext _db;
    public NotificationOutbox(CinemaBookingDbContext db) => _db = db;
    public Task EnqueueBookingSuccessAsync(int bookingId, CancellationToken ct = default) => EnqueueAsync(
        $"BookingSuccess:{bookingId}", "BookingSuccess", bookingId, null, null, ct);
    public Task EnqueueRefundCompletedAsync(int bookingId, decimal amount, DateTime completedAt, CancellationToken ct = default) => EnqueueAsync(
        $"RefundCompleted:{bookingId}", "RefundCompleted", bookingId, amount, completedAt, ct);
    private async Task EnqueueAsync(string eventId, string eventType, int referenceId, decimal? amount, DateTime? occurredAt, CancellationToken ct)
    {
        if (await _db.NotificationOutbox.AnyAsync(x => x.EventId == eventId, ct)) return;
        _db.NotificationOutbox.Add(new Domain.Entities.NotificationOutbox { EventId = eventId, EventType = eventType,
            ReferenceId = referenceId, Amount = amount, OccurredAt = occurredAt, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }
}
