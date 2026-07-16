namespace CinemaBooking.Application.Notifications;

public sealed record EmailLogItem(int EmailLogId, int? UserId, string RecipientEmail, string Subject,
    string EventType, string DeliveryStatus, int RetryCount, string? ErrorMessage,
    DateTimeOffset? SentAt, DateTimeOffset CreatedAt);

public sealed record EmailLogPage(IReadOnlyList<EmailLogItem> Items, int Page, int PageSize,
    int TotalItems, int TotalPages);
