using System.Net;
using System.Net.Mail;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CinemaBooking.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailSettings> emailSettings,
        ILogger<SmtpEmailSender> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_emailSettings.Host))
        {
            _logger.LogWarning("Email host is not configured; skipping email to {ToEmail}", toEmail);
            return false;
        }

        _logger.LogInformation("Sending email to {ToEmail} via {Host}:{Port}", toEmail, _emailSettings.Host, _emailSettings.Port);

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromAddress, _emailSettings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
            {
                EnableSsl = _emailSettings.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
            {
                client.Credentials = new NetworkCredential(
                    _emailSettings.Username,
                    _emailSettings.Password);
            }

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email successfully sent to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
        {
            _logger.LogWarning(ex, "Failed to send email to {ToEmail}. Host={Host}, Port={Port}, EnableSsl={EnableSsl}",
                toEmail,
                _emailSettings.Host,
                _emailSettings.Port,
                _emailSettings.EnableSsl);
            return false;
        }
    }
}
