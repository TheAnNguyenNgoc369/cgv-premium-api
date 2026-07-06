using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;

namespace CinemaBooking.Infrastructure.Email;

public sealed class EmailQueue : IEmailQueue
{
    private readonly CinemaBookingDbContext _dbContext;
    private readonly EmailQueueChannel _channel;

    internal EmailQueue(CinemaBookingDbContext dbContext, EmailQueueChannel channel)
    {
        _dbContext = dbContext;
        _channel = channel;
    }

    public async Task<int> EnqueueAsync(
        int? userId,
        string toEmail,
        string eventType,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        var emailLog = new EmailLog
        {
            UserID = userId,
            ToEmail = toEmail,
            EventType = eventType,
            DeliveryStatus = "pending",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _channel.WriteAsync(
            new EmailQueueItem(emailLog.EmailLogID, toEmail, subject, htmlBody),
            cancellationToken);

        return emailLog.EmailLogID;
    }
}
