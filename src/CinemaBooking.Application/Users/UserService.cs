using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Users;

public sealed class UserService : IUserService
{
    private const string AvatarFolder = "cgvp/avatars";

    private readonly IUserRepository _userRepository;
    private readonly IImageStorageService _imageStorageService;

    public UserService(
        IUserRepository userRepository,
        IImageStorageService imageStorageService)
    {
        _userRepository = userRepository;
        _imageStorageService = imageStorageService;
    }

    public Task<User?> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, User? User)> UpdateProfileAsync(
        int userId,
        string fullName,
        string? phone,
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
        var updatedUser = await _userRepository.UpdateProfileAsync(
            userId,
            normalizedFullName,
            normalizedPhone,
            cancellationToken);

        return updatedUser is null
            ? (false, "User not found", null)
            : (true, null, updatedUser);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, User? User)> UploadAvatarAsync(
        int userId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        var validationError = ImageFileValidator.Validate(fileName, contentType, fileSize);

        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return (false, "User not found", null);
        }

        var previousPublicId = user.AvatarPublicId;
        var uploadResult = await _imageStorageService.UploadImageAsync(
            imageStream,
            fileName,
            AvatarFolder,
            cancellationToken);

        var updatedUser = await _userRepository.UpdateAvatarAsync(
            userId,
            uploadResult.SecureUrl,
            uploadResult.PublicId,
            cancellationToken);

        if (updatedUser is null)
        {
            await _imageStorageService.DeleteImageAsync(uploadResult.PublicId, cancellationToken);
            return (false, "User not found", null);
        }

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await _imageStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return (true, null, updatedUser);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, User? User)> DeleteAvatarAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return (false, "User not found", null);
        }

        var previousPublicId = user.AvatarPublicId;
        var updatedUser = await _userRepository.UpdateAvatarAsync(
            userId,
            null,
            null,
            cancellationToken);

        if (updatedUser is null)
        {
            return (false, "User not found", null);
        }

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await _imageStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return (true, null, updatedUser);
    }

    public Task<Wallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetWalletByUserIdAsync(userId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return (false, "User not found");
        }

        var deleted = await _userRepository.DeleteAsync(userId, cancellationToken);

        if (!deleted)
        {
            return (false, "User has related records and cannot be deleted");
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarPublicId))
        {
            await _imageStorageService.DeleteImageAsync(user.AvatarPublicId, cancellationToken);
        }

        return (true, null);
    }
}
