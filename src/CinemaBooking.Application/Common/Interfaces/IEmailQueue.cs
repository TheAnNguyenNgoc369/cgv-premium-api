namespace CinemaBooking.Application.Common.Interfaces;

public interface IEmailQueue
{
    Task<int> EnqueueAsync(
        int? userId,
        string toEmail,
        string eventType,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    Task<int> EnqueueAsync(
        int? userId,
        string toEmail,
        string eventType,
        string subject,
        string htmlBody,
        IReadOnlyCollection<EmailInlineImage>? inlineImages = null,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(userId, toEmail, eventType, subject, htmlBody, cancellationToken);
}
