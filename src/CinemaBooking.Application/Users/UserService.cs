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

        if (!string.IsNullOrWhiteSpace(user.AvatarURL)
            && string.IsNullOrWhiteSpace(user.AvatarPublicId))
        {
            return (
                false,
                "Avatar cannot be replaced because its Cloudinary public ID is missing",
                null);
        }

        var previousPublicId = user.AvatarPublicId;
        var uploadResult = await _imageStorageService.UploadImageAsync(
            imageStream,
            fileName,
            AvatarFolder,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            try
            {
                await _imageStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var cleanupError = await TryDeleteImageAsync(uploadResult.PublicId, cancellationToken);
                var errorMessage = $"Failed to delete previous avatar from Cloudinary: {exception.Message}";

                if (cleanupError is not null)
                {
                    errorMessage += $" The newly uploaded avatar could not be cleaned up: {cleanupError}";
                }

                return (false, errorMessage, null);
            }
        }

        User? updatedUser;

        try
        {
            updatedUser = await _userRepository.UpdateAvatarAsync(
                userId,
                uploadResult.SecureUrl,
                uploadResult.PublicId,
                cancellationToken);
        }
        catch
        {
            await TryDeleteImageAsync(uploadResult.PublicId, CancellationToken.None);
            throw;
        }

        if (updatedUser is null)
        {
            var cleanupError = await TryDeleteImageAsync(uploadResult.PublicId, cancellationToken);
            return cleanupError is null
                ? (false, "User not found", null)
                : (false, $"User not found. The newly uploaded avatar could not be cleaned up: {cleanupError}", null);
        }

        return (true, null, updatedUser);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, User? User, string? Message)> DeleteAvatarAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return (false, "User not found", null, null);
        }

        var hasAvatarUrl = !string.IsNullOrWhiteSpace(user.AvatarURL);
        var hasPublicId = !string.IsNullOrWhiteSpace(user.AvatarPublicId);

        if (!hasAvatarUrl && !hasPublicId)
        {
            return (true, null, user, "No avatar to delete");
        }

        if (!hasPublicId)
        {
            return (
                false,
                "Avatar cannot be deleted because its Cloudinary public ID is missing",
                null,
                null);
        }

        try
        {
            await _imageStorageService.DeleteImageAsync(user.AvatarPublicId!, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (
                false,
                $"Failed to delete avatar from Cloudinary: {exception.Message}",
                null,
                null);
        }

        var updatedUser = await _userRepository.UpdateAvatarAsync(
            userId,
            null,
            null,
            cancellationToken);

        if (updatedUser is null)
        {
            return (false, "User not found", null, null);
        }

        return (true, null, updatedUser, "Avatar deleted successfully");
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

    private async Task<string?> TryDeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _imageStorageService.DeleteImageAsync(publicId, cancellationToken);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return exception.Message;
        }
    }
}
