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

    // Manager/Admin notifications
    Task EnqueueRevenueAnomalyAsync(int cinemaId, string message, CancellationToken cancellationToken = default);
    Task EnqueueDailySummaryAsync(int? cinemaId, string message, CancellationToken cancellationToken = default);
    Task EnqueueTopSellingMovieAsync(int movieId, string message, CancellationToken cancellationToken = default);
    Task EnqueueLowPerformingMovieAsync(int movieId, string message, CancellationToken cancellationToken = default);
    Task EnqueueMovieEndingSoonAsync(int movieId, string message, int daysRemaining, CancellationToken cancellationToken = default);
    Task EnqueueShowtimeSoldOutAsync(int showtimeId, string message, CancellationToken cancellationToken = default);
    Task EnqueueLowOccupancyShowtimeAsync(int showtimeId, string message, CancellationToken cancellationToken = default);
    Task EnqueueVoucherExpiringSoonAsync(int voucherId, string message, int daysRemaining, CancellationToken cancellationToken = default);
    Task EnqueueVoucherOutOfStockAsync(int voucherId, string message, CancellationToken cancellationToken = default);
    Task EnqueueNewCustomerSummaryAsync(string message, CancellationToken cancellationToken = default);
    Task EnqueuePaymentIssueAsync(string message, string? paymentMethod = null, CancellationToken cancellationToken = default);
    Task EnqueueRoomCreatedAsync(int roomId, string message, CancellationToken cancellationToken = default);
    Task EnqueueRoomUpdatedAsync(int roomId, string message, CancellationToken cancellationToken = default);
    Task EnqueueRoomInactiveAsync(int roomId, string message, CancellationToken cancellationToken = default);
    Task EnqueueShowtimeStartingSoonAsync(int showtimeId, string message, int minutesRemaining, CancellationToken cancellationToken = default);
    Task EnqueueShowtimeCreatedAsync(int showtimeId, string message, CancellationToken cancellationToken = default);
    Task EnqueueShowtimeUpdatedAsync(int showtimeId, string message, CancellationToken cancellationToken = default);
    Task EnqueueShowtimeDeletedAsync(int showtimeId, string message, CancellationToken cancellationToken = default);
}
