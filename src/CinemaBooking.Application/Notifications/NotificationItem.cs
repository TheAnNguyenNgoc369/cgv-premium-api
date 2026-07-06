namespace CinemaBooking.Application.Notifications;

public sealed record NotificationItem(int NotificationId, string Title, string Message, string Type,
    string EventType, string? ReferenceType, int? ReferenceId, string? ActionUrl, bool IsRead,
    DateTime? ReadAt, DateTime CreatedAt);
