using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetProfileByIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<User?> LookupCustomerAsync(
        string? email,
        string? phone,
        string? barcode,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> PhoneExistsAsync(
        string phone,
        int? excludingUserId = null,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> UpdateProfileAsync(
        int userId,
        string fullName,
        string? phone,
        CancellationToken cancellationToken = default);

    Task<User?> UpdateAvatarAsync(
        int userId,
        string? avatarUrl,
        string? avatarPublicId,
        CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int userId, CancellationToken cancellationToken = default);

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

    Task<PasswordResetToken?> GetLatestPasswordResetTokenAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task ReplaceUnusedPasswordResetTokensAsync(
        int userId,
        PasswordResetToken resetToken,
        CancellationToken cancellationToken = default);

    Task ReplaceUnverifiedEmailVerificationTokensAsync(
        int userId,
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default);

    Task DeleteEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task DeletePasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<bool> TryResetPasswordAsync(
        string token,
        string passwordHash,
        DateTime resetAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryIncrementTokenVersionAsync(
        int userId,
        int expectedTokenVersion,
        CancellationToken cancellationToken = default);

    Task<bool> TryUpdatePasswordHashAsync(
        int userId,
        string expectedPasswordHash,
        string newPasswordHash,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
