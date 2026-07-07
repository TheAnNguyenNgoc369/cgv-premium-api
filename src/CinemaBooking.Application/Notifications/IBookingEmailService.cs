namespace CinemaBooking.Application.Notifications;

public interface IBookingEmailService
{
    Task QueueBookingConfirmedAsync(int bookingId, CancellationToken cancellationToken = default);
    Task QueueRefundProcessedAsync(int bookingId, decimal refundAmount, DateTime completedAt,
        CancellationToken cancellationToken = default);
}
