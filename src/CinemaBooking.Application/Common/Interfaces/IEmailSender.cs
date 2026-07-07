namespace CinemaBooking.Application.Common.Interfaces;

public interface IEmailSender
{
    Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        IReadOnlyCollection<EmailInlineImage>? inlineImages = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, subject, htmlBody, cancellationToken);
}
