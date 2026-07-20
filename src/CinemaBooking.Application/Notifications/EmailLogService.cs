using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Time;

namespace CinemaBooking.Application.Notifications;

public sealed class EmailLogService : IEmailLogService
{
    private static readonly HashSet<string> DeliveryStatuses = ["pending", "queued", "processing", "sent", "failed", "retrying"];

    private readonly IEmailLogRepository _repository;

    public EmailLogService(IEmailLogRepository repository) => _repository = repository;

    public async Task<EmailLogPage> GetAsync(int? userId, string? recipientEmail, string? eventType,
        string? deliveryStatus, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) throw new ArgumentException("page must be at least 1.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("pageSize must be between 1 and 100.");
        if (fromDate > toDate) throw new ArgumentException("fromDate must not be later than toDate.");

        var normalizedStatus = Normalize(deliveryStatus);
        if (normalizedStatus is not null && !DeliveryStatuses.Contains(normalizedStatus))
            throw new ArgumentException("deliveryStatus is invalid.");

        var fromUtc = fromDate.HasValue ? VietnamTime.GetUtcDayRange(fromDate.Value).FromUtc : (DateTime?)null;
        var toUtc = toDate.HasValue ? VietnamTime.GetUtcDayRange(toDate.Value).ToUtc : (DateTime?)null;

        var result = await _repository.GetAsync(userId, Normalize(recipientEmail), Normalize(eventType),
            normalizedStatus, fromUtc, toUtc, page, pageSize, cancellationToken);

        return new EmailLogPage(result.Items.Select(Map).ToList(), page, pageSize, result.Total,
            (int)Math.Ceiling(result.Total / (double)pageSize));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static EmailLogItem Map(EmailLog log) => new(log.EmailLogID, log.UserID, log.ToEmail,
        log.Subject, log.EventType, log.DeliveryStatus, log.RetryCount, log.LastError,
        log.SentAt.HasValue ? VietnamTime.FromUtc(log.SentAt.Value) : null,
        VietnamTime.FromUtc(log.CreatedAt));
}
