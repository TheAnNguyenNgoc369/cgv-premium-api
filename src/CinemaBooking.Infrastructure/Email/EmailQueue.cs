using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using System.Text.Json;

namespace CinemaBooking.Infrastructure.Email;

public sealed class EmailQueue : IEmailQueue
{
    private readonly CinemaBookingDbContext _dbContext;
    public EmailQueue(CinemaBookingDbContext dbContext) => _dbContext = dbContext;

    public Task<int> EnqueueAsync(
        int? userId,
        string toEmail,
        string eventType,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(userId, toEmail, eventType, subject, htmlBody, null, cancellationToken);

    public async Task<int> EnqueueAsync(
        int? userId,
        string toEmail,
        string eventType,
        string subject,
        string htmlBody,
        IReadOnlyCollection<EmailInlineImage>? inlineImages = null,
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
            Subject = subject,
            HtmlBody = htmlBody,
            InlineImagesJson = inlineImages is null ? null : JsonSerializer.Serialize(inlineImages),
            EventType = eventType,
            DeliveryStatus = "pending",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return emailLog.EmailLogID;
    }
}
