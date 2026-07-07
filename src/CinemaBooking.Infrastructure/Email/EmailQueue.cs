using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CinemaBooking.Infrastructure.Email;

public sealed class EmailQueue : IEmailQueue
{
    private readonly CinemaBookingDbContext _dbContext;
    public EmailQueue(CinemaBookingDbContext dbContext) => _dbContext = dbContext;

    public Task<int> EnqueueAsync(
        int? userId,
        string toEmail,
        string eventId,
        string eventType,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(userId, toEmail, eventId, eventType, subject, htmlBody, null, cancellationToken);

    public async Task<int> EnqueueAsync(
        int? userId,
        string toEmail,
        string eventId,
        string eventType,
        string subject,
        string htmlBody,
        IReadOnlyCollection<EmailInlineImage>? inlineImages = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        var existingEmailId = await _dbContext.EmailLogs
            .Where(email => email.EventId == eventId)
            .Select(email => email.EmailLogID)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingEmailId != 0)
            return existingEmailId;

        var emailLog = new EmailLog
        {
            UserID = userId,
            ToEmail = toEmail,
            EventId = eventId,
            Subject = subject,
            HtmlBody = htmlBody,
            InlineImagesJson = inlineImages is null ? null : JsonSerializer.Serialize(inlineImages),
            EventType = eventType,
            DeliveryStatus = "pending",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.EmailLogs.Add(emailLog);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(emailLog).State = EntityState.Detached;
            existingEmailId = await _dbContext.EmailLogs
                .Where(email => email.EventId == eventId)
                .Select(email => email.EmailLogID)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingEmailId != 0)
                return existingEmailId;

            throw;
        }

        return emailLog.EmailLogID;
    }
}
