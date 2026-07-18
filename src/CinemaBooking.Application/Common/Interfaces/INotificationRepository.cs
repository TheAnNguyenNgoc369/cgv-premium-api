using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task<(List<Notification> Items, int Total)> GetAsync(int userId, int page, int pageSize, bool? isRead,
        string? type, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken);
    Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken);
    Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<int> MarkAllReadAsync(int userId, DateTime now, CancellationToken cancellationToken);
    Task<int> DeleteReadAsync(int userId, DateTime now, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);
}
