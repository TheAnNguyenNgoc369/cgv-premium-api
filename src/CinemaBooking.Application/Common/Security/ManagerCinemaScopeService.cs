using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Common.Security;

public sealed class ManagerCinemaScopeService : IManagerCinemaScopeService
{
    private readonly IUserRepository _userRepository;

    public ManagerCinemaScopeService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<int?> GetAssignedCinemaIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user?.Role == Roles.Manager ? user.CinemaID : null;
    }
}
