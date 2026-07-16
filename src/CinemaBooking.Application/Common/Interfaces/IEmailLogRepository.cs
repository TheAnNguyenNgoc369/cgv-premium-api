using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IEmailLogRepository
{
    Task<(List<EmailLog> Items, int Total)> GetAsync(int? userId, string? recipientEmail, string? eventType,
        string? deliveryStatus, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize,
        CancellationToken cancellationToken);
}
