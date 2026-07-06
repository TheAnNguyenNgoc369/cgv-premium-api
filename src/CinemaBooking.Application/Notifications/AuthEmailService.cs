using CinemaBooking.Application.Common.Interfaces;

namespace CinemaBooking.Application.Notifications;

public sealed class AuthEmailService : IAuthEmailService
{
    private readonly IEmailQueue _emailQueue;

    public AuthEmailService(IEmailQueue emailQueue)
    {
        _emailQueue = emailQueue;
    }

    public Task QueueVerificationAsync(
        int userId,
        string email,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        QueueAsync(userId, email, "register", subject, htmlBody, cancellationToken);

    public Task QueuePasswordResetAsync(
        int userId,
        string email,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        QueueAsync(userId, email, "forgot_password", subject, htmlBody, cancellationToken);

    private async Task QueueAsync(
        int userId,
        string email,
        string eventType,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        await _emailQueue.EnqueueAsync(
            userId, email, eventType, subject, htmlBody, cancellationToken);
    }
}
