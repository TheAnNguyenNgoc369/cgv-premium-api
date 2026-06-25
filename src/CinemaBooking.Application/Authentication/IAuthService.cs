using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Authentication;

public interface IAuthService
{
    Task<(bool Succeeded, string? ErrorMessage, int? UserId, bool VerificationEmailSent)> RegisterAsync(
        string fullName,
        string email,
        string phone,
        string password,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, User? User)> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Message, bool VerificationEmailSent, int? RetryAfterSeconds)> ResendVerificationEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> VerifyEmailAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Message, bool EmailSent, int? RetryAfterSeconds)> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> ResetPasswordAsync(
        string token,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken = default);
}
