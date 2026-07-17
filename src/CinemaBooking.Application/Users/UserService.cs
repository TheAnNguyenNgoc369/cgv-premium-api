using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Application.Users;

public sealed class UserService : IUserService
{
    private const string AvatarFolder = "cgvp/avatars";
    private const string AvatarUpdateFailedMessage = "Avatar could not be updated. Please try again later.";
    private const string AvatarDeleteFailedMessage = "Avatar could not be deleted. Please try again later.";

    private readonly IUserRepository _userRepository;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IUserVoucherRepository userVoucherRepository,
        IImageStorageService imageStorageService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _userVoucherRepository = userVoucherRepository;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    public Task<User?> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetProfileByIdAsync(userId, cancellationToken);
    }

    public Task<User?> LookupCustomerAsync(
        string? email,
        string? phone,
        string? barcode,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();

        return _userRepository.LookupCustomerAsync(normalizedEmail, normalizedPhone, normalizedBarcode, cancellationToken);
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

        if (normalizedPhone is not null
            && await _userRepository.PhoneExistsAsync(normalizedPhone, userId, cancellationToken))
        {
            return (false, "Phone is already in use.", null);
        }

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
            await TryDeleteImageAsync(uploadResult.PublicId, CreateCorrelationId(), CancellationToken.None);
            throw;
        }

        if (updatedUser is null)
        {
            var correlationId = CreateCorrelationId();

            await TryDeleteImageAsync(uploadResult.PublicId, correlationId, cancellationToken);

            return (false, "User not found", null);
        }

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            try
            {
                await _imageStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    exception,
                    "Failed to delete previous avatar after avatar update. CorrelationId: {CorrelationId}",
                    CreateCorrelationId());
            }
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
            var correlationId = CreateCorrelationId();

            _logger.LogError(
                exception,
                "Failed to delete avatar. CorrelationId: {CorrelationId}",
                correlationId);

            return (
                false,
                AvatarDeleteFailedMessage,
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

    public async Task<(bool Succeeded, string? ErrorMessage)> ChangePasswordAsync(
        int userId,
        string oldPassword,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return (false, "User not found");
        }

        if (!PasswordHasher.Verify(oldPassword, user.PasswordHash, out _))
        {
            return (false, "Current password is incorrect.");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return (false, "Confirm password does not match.");
        }

        if (PasswordHasher.Verify(newPassword, user.PasswordHash, out _))
        {
            return (false, "New password must be different from the current password.");
        }

        var updated = await _userRepository.TryUpdatePasswordHashAsync(
            userId,
            user.PasswordHash,
            PasswordHasher.Hash(newPassword),
            cancellationToken);

        return updated
            ? (true, null)
            : (false, "Password could not be changed. Please try again.");
    }

    private async Task TryDeleteImageAsync(
        string publicId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _imageStorageService.DeleteImageAsync(publicId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Failed to clean up uploaded avatar. CorrelationId: {CorrelationId}",
                correlationId);
        }
    }

    private static string CreateCorrelationId()
    {
        return System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
    }

    public async Task<List<UserVoucher>> GetUserRedeemableVouchersAsync(int userId, CancellationToken ct)
    {
        return await _userVoucherRepository.GetUserVouchersAsync(userId, ct);
    }
}
