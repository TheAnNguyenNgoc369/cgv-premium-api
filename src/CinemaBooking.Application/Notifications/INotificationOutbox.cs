namespace CinemaBooking.Application.Notifications;

public interface INotificationOutbox
{
    Task EnqueueBookingSuccessAsync(int bookingId, CancellationToken cancellationToken = default);
    Task EnqueueRefundCompletedAsync(int bookingId, decimal amount, DateTime completedAt,
        CancellationToken cancellationToken = default);
    Task EnqueueWalletRefundAsync(int userId, decimal amount, DateTime occurredAt,
        CancellationToken cancellationToken = default);
    Task EnqueueWalletPaymentAsync(int userId, decimal amount, DateTime occurredAt,
        CancellationToken cancellationToken = default);
}
