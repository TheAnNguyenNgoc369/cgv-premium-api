using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task AddUserWithWalletAsync(User user, Wallet wallet, CancellationToken cancellationToken = default);

    Task AddUserWithWalletAndVerificationTokenAsync(
        User user,
        Wallet wallet,
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
