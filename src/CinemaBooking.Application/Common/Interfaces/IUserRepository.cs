using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> UpdateProfileAsync(
        int userId,
        string fullName,
        string? phone,
        string? avatarUrl,
        CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task AddUserWithWalletAsync(User user, Wallet wallet, CancellationToken cancellationToken = default);

    Task AddUserWithWalletAndVerificationTokenAsync(
        User user,
        Wallet wallet,
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default);

    Task AddEmailVerificationTokenAsync(
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationToken?> GetLatestEmailVerificationTokenAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddPasswordResetTokenAsync(
        PasswordResetToken resetToken,
        CancellationToken cancellationToken = default);

    Task ReplaceUnverifiedEmailVerificationTokensAsync(
        int userId,
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetPasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
