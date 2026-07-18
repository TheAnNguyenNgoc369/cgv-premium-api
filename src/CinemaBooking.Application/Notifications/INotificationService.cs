namespace CinemaBooking.Application.Notifications;

public interface INotificationService
{
    Task<NotificationPage> GetAsync(int userId, int page, int pageSize, bool? isRead, string? type,
        DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<NotificationLookupResult> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<NotificationMutationResult> MarkReadAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<NotificationMutationResult> DeleteAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<int> DeleteReadAsync(int userId, CancellationToken cancellationToken = default);
    Task SendToRolesAsync(IEnumerable<string> roles, string title, string message, string type, string eventType, string? referenceType = null, int? referenceId = null, string? actionUrl = null, CancellationToken cancellationToken = default);
}
