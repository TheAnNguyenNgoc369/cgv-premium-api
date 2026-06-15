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

    public async Task<(bool Succeeded, string? ErrorMessage, User? User)> UpdateProfileAsync(
        int userId,
        string fullName,
        string? phone,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (false, "Full name is required", null);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return (false, "User not found", null);
        }

        var normalizedFullName = fullName.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone)
            ? null
            : phone.Trim();
        var normalizedAvatarUrl = string.IsNullOrWhiteSpace(avatarUrl)
            ? null
            : avatarUrl.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedPhone)
            && await _userRepository.PhoneExistsForAnotherUserAsync(
                normalizedPhone,
                userId,
                cancellationToken))
        {
            return (false, "Phone number is already registered", null);
        }

        var updatedUser = await _userRepository.UpdateProfileAsync(
            userId,
            normalizedFullName,
            normalizedPhone,
            normalizedAvatarUrl,
            cancellationToken);

        return updatedUser is null
            ? (false, "User not found", null)
            : (true, null, updatedUser);
    }

    public Task<Wallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetWalletByUserIdAsync(userId, cancellationToken);
    }
}
