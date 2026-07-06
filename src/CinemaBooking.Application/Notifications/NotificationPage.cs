namespace CinemaBooking.Application.Notifications;

public sealed record NotificationPage(IReadOnlyList<NotificationItem> Items, int Page, int PageSize, int TotalItems);
