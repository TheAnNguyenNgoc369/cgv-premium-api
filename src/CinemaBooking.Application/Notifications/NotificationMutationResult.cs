namespace CinemaBooking.Application.Notifications;

public sealed record NotificationMutationResult(bool Succeeded, string? Error, string? ErrorType = null);
