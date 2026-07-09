namespace CinemaBooking.Application.Notifications;

public interface IAuthEmailService
{
    Task QueueVerificationAsync(
        int userId,
        string email,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    Task QueuePasswordResetAsync(
        int userId,
        string email,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
