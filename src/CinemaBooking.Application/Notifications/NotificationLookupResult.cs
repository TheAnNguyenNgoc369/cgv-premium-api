namespace CinemaBooking.Application.Notifications;

public sealed record NotificationLookupResult(NotificationItem? Notification, string? ErrorType = null);
