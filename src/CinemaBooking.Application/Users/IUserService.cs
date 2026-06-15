using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Users;

public interface IUserService
{
    Task<User?> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, User? User)> UpdateProfileAsync(
        int userId,
        string fullName,
        string? phone,
        string? avatarUrl,
        CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default);
}
