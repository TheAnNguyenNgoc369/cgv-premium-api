using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Users;

public interface IUserService
{
    Task<User?> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default);
}
