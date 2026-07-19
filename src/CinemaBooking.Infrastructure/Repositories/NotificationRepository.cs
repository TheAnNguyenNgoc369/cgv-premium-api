using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly CinemaBookingDbContext _db;
    public NotificationRepository(CinemaBookingDbContext db) => _db = db;
    public async Task<(List<Notification> Items, int Total)> GetAsync(int userId, int page, int pageSize, bool? isRead,
        string? type, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var q = _db.Notifications.AsNoTracking().Where(x => x.UserID == userId && x.DeletedAt == null);
        if (isRead.HasValue) q = q.Where(x => x.IsRead == isRead);
        if (type is not null) q = q.Where(x => x.Type == type);
        if (fromDate.HasValue) q = q.Where(x => x.CreatedAt >= fromDate);
        if (toDate.HasValue) q = q.Where(x => x.CreatedAt <= toDate);
        var total = await q.CountAsync(ct);
        return (await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), total);
    }
    public Task<int> CountUnreadAsync(int userId, CancellationToken ct) => _db.Notifications.CountAsync(x => x.UserID == userId && !x.IsRead && x.DeletedAt == null, ct);
    public Task<Notification?> GetByIdAsync(int id, CancellationToken ct) => _db.Notifications.FirstOrDefaultAsync(x => x.NotificationID == id && x.DeletedAt == null, ct);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    public Task<int> MarkAllReadAsync(int userId, DateTime now, CancellationToken ct) => _db.Notifications.Where(x => x.UserID == userId && !x.IsRead && x.DeletedAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAt, now), ct);
    public Task<int> DeleteReadAsync(int userId, DateTime now, CancellationToken ct) => _db.Notifications.Where(x => x.UserID == userId && x.IsRead && x.DeletedAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedAt, now), ct);
    public Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        _db.Notifications.AddRange(notifications);
        return _db.SaveChangesAsync(ct);
    }
}
