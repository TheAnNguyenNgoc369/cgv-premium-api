using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private static readonly HashSet<string> Types = ["system", "promotion", "refund", "payment", "booking", "account"];
    private readonly INotificationRepository _repository;
    private readonly IUserRepository _userRepository;

    public NotificationService(
        INotificationRepository repository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public async Task<NotificationPage> GetAsync(int userId, int page, int pageSize, bool? isRead, string? type,
        DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var normalizedType = type?.Trim().ToLowerInvariant();
        if (normalizedType is not null && !Types.Contains(normalizedType)) throw new ArgumentException("type is invalid.");
        if (page < 1) throw new ArgumentException("page must be at least 1.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("pageSize must be between 1 and 100.");
        if (fromDate > toDate) throw new ArgumentException("fromDate must not be later than toDate.");
        var result = await _repository.GetAsync(userId, page, pageSize, isRead, normalizedType, fromDate, toDate, ct);
        return new NotificationPage(result.Items.Select(Map).ToList(), page, pageSize, result.Total);
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default) => _repository.CountUnreadAsync(userId, ct);

    public async Task<NotificationLookupResult> GetByIdAsync(int userId, int id, CancellationToken ct = default)
    {
        var value = await _repository.GetByIdAsync(id, ct);
        if (value is null) return new(null, "not_found");
        return value.UserID == userId ? new(Map(value)) : new(null, "forbidden");
    }

    public async Task<NotificationMutationResult> MarkReadAsync(int userId, int id, CancellationToken ct = default) => await Mutate(userId, id, false, ct);

    public Task<int> MarkAllReadAsync(int userId, CancellationToken ct = default) => _repository.MarkAllReadAsync(userId, DateTime.UtcNow, ct);

    public async Task<NotificationMutationResult> DeleteAsync(int userId, int id, CancellationToken ct = default) => await Mutate(userId, id, true, ct);

    public Task<int> DeleteReadAsync(int userId, CancellationToken ct = default) => _repository.DeleteReadAsync(userId, DateTime.UtcNow, ct);

    public async Task SendToRolesAsync(
        IEnumerable<string> roles,
        string title,
        string message,
        string type,
        string eventType,
        string? referenceType = null,
        int? referenceId = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetUsersByRolesAsync(roles, cancellationToken);
        var now = DateTime.UtcNow;

        var notifications = users.Select(user => new Notification
        {
            UserID = user.UserID,
            Title = title,
            Message = message,
            Type = type,
            EventType = eventType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        if (notifications.Count > 0)
        {
            await _repository.AddRangeAsync(notifications, cancellationToken);
        }
    }

    private async Task<NotificationMutationResult> Mutate(int userId, int id, bool delete, CancellationToken ct)
    {
        var value = await _repository.GetByIdAsync(id, ct);
        if (value is null) return new(false, "Notification not found.", "not_found");
        if (value.UserID != userId) return new(false, "Forbidden.", "forbidden");
        if (delete) value.DeletedAt = DateTime.UtcNow;
        else { value.IsRead = true; value.ReadAt ??= DateTime.UtcNow; }
        await _repository.SaveChangesAsync(ct); return new(true, null);
    }

    private static NotificationItem Map(Notification x) => new(x.NotificationID, x.Title, x.Message, x.Type,
        x.EventType, x.ReferenceType, x.ReferenceId, x.ActionUrl, x.IsRead, x.ReadAt, x.CreatedAt);
}
