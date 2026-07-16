namespace CinemaBooking.Application.Notifications;

public interface IEmailLogService
{
    Task<EmailLogPage> GetAsync(int? userId, string? recipientEmail, string? eventType, string? deliveryStatus,
        DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
}
