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

    Task<(bool Succeeded, string? ErrorMessage, bool VerificationEmailSent)> ResendVerificationEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default);
}
