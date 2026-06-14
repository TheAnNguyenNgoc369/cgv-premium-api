using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task AddUserWithWalletAsync(User user, Wallet wallet, CancellationToken cancellationToken = default);
}
