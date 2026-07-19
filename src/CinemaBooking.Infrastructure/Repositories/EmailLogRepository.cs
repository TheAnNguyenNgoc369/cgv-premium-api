using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class EmailLogRepository : IEmailLogRepository
{
    private readonly CinemaBookingDbContext _db;

    public EmailLogRepository(CinemaBookingDbContext db) => _db = db;

    public async Task<(List<EmailLog> Items, int Total)> GetAsync(int? userId, string? recipientEmail,
        string? eventType, string? deliveryStatus, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.EmailLogs.AsNoTracking();

        if (userId.HasValue) query = query.Where(log => log.UserID == userId);
        if (recipientEmail is not null) query = query.Where(log => log.ToEmail.ToLower().Contains(recipientEmail));
        if (eventType is not null) query = query.Where(log => log.EventType == eventType);
        if (deliveryStatus is not null) query = query.Where(log => log.DeliveryStatus == deliveryStatus);
        if (fromUtc.HasValue) query = query.Where(log => log.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(log => log.CreatedAt < toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
