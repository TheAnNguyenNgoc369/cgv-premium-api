namespace CinemaBooking.Application.Notifications;

public interface INotificationOutbox
{
    Task EnqueueBookingSuccessAsync(int bookingId, CancellationToken cancellationToken = default);
    Task EnqueueRefundCompletedAsync(int bookingId, decimal amount, DateTime completedAt,
        CancellationToken cancellationToken = default);
}
