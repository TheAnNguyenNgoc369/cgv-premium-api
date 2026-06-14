using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<User?> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public Task<Wallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetWalletByUserIdAsync(userId, cancellationToken);
    }
}
