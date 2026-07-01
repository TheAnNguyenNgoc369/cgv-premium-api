using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Application.AdminUsers;

public sealed class AdminUserService : IAdminUserService
{
    private const string AvatarFolder = "cgvp/avatars";
    private readonly IAdminUserRepository _repository;
    private readonly IImageStorageService _imageStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IAdminUserRepository repository,
        IImageStorageService imageStorageService,
        IUnitOfWork unitOfWork,
        ILogger<AdminUserService> logger)
    {
        _repository = repository;
        _imageStorageService = imageStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AdminUserResult<AdminUserPageResult>> GetUsersAsync(
        string? search, string? role, string? status, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var roleResult = NormalizeOptional(role, DatabaseEnumMappings.UserRoles, "Invalid role");
        if (!roleResult.Succeeded)
            return AdminUserResult<AdminUserPageResult>.Failure(
                AdminUserErrorType.Validation, roleResult.Error!);

        var statusResult = NormalizeOptional(status, DatabaseEnumMappings.UserStatuses, "Invalid status");
        if (!statusResult.Succeeded)
            return AdminUserResult<AdminUserPageResult>.Failure(
                AdminUserErrorType.Validation, statusResult.Error!);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        var result = await _repository.GetPageAsync(
            search?.Trim(), roleResult.Value, statusResult.Value,
            page, pageSize, cancellationToken);
        return AdminUserResult<AdminUserPageResult>.Success(
            new AdminUserPageResult(result.Items, page, pageSize, result.TotalItems));
    }

    public async Task<AdminUserResult<User>> CreateAsync(
        int adminId, AdminUserCreateCommand command, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var fieldError = ValidateIdentityFields(command.FullName, command.Email, command.Phone);
        if (fieldError is not null) return Validation<User>(fieldError);
        if (!IsValidAdminPassword(command.Password)) return Validation<User>(PasswordErrorMessage);

        var roleResult = NormalizeRequired(command.Role, DatabaseEnumMappings.UserRoles, "Invalid role");
        if (!roleResult.Succeeded) return Validation<User>(roleResult.Error!);
        var statusResult = string.IsNullOrWhiteSpace(command.Status)
            ? (true, (string?)UserStatuses.Active, (string?)null)
            : NormalizeRequired(command.Status, DatabaseEnumMappings.UserStatuses, "Invalid status");
        if (!statusResult.Item1) return Validation<User>(statusResult.Item3!);

        var cinemaError = await ValidateCinemaAsync(roleResult.Value!, command.CinemaId, cancellationToken);
        if (cinemaError is not null) return Validation<User>(cinemaError);

        var email = command.Email.Trim();
        if (await _repository.EmailExistsAsync(email, cancellationToken: cancellationToken))
            return Conflict<User>("Email is already registered");

        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = command.FullName.Trim(),
            Email = email,
            Phone = command.Phone.Trim(),
            PasswordHash = PasswordHasher.Hash(command.Password),
            Role = roleResult.Value!,
            Status = statusResult.Item2!,
            CinemaID = command.CinemaId,
            EmailVerifiedAt = now,
            TotalPoints = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        var log = BuildLog(adminId, null, AdminActionTypes.CreateUser,
            $"Created user with role {user.Role} and status {user.Status}.", ipAddress);
        await _repository.AddAsync(user, new Wallet { Balance = 0m }, log, cancellationToken);
        return AdminUserResult<User>.Success(user);
    }

    public async Task<AdminUserResult<User>> UpdateAsync(
        int adminId, int userId, AdminUserUpdateCommand command, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(userId, cancellationToken);
        if (existing is null) return NotFound<User>();
        var fieldError = ValidateIdentityFields(command.FullName, command.Email, command.Phone);
        if (fieldError is not null) return Validation<User>(fieldError);
        var cinemaError = await ValidateCinemaAsync(existing.Role, command.CinemaId, cancellationToken);
        if (cinemaError is not null) return Validation<User>(cinemaError);

        var email = command.Email.Trim();
        if (await _repository.EmailExistsAsync(email, userId, cancellationToken))
            return Conflict<User>("Email is already registered");

        var changedFields = new List<string>();
        if (existing.FullName != command.FullName.Trim()) changedFields.Add("FullName");
        if (!string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase)) changedFields.Add("Email");
        if (existing.Phone != command.Phone.Trim()) changedFields.Add("Phone");
        if (existing.CinemaID != command.CinemaId) changedFields.Add("CinemaID");
        var description = changedFields.Count == 0
            ? "Profile update requested; no values changed."
            : $"Updated fields: {string.Join(", ", changedFields)}.";
        var log = BuildLog(adminId, userId, AdminActionTypes.UpdateUser, description, ipAddress);
        var user = await _repository.UpdateAsync(
            userId, command.FullName.Trim(), email, command.Phone.Trim(), command.CinemaId,
            log, cancellationToken);
        return user is null ? NotFound<User>() : AdminUserResult<User>.Success(user);
    }

    public async Task<AdminUserResult<User>> ChangeRoleAsync(
        int adminId, int userId, string role, int? cinemaId, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(userId, cancellationToken);
        if (existing is null) return NotFound<User>();
        if (adminId == userId) return Forbidden<User>("Admin cannot change their own role");
        if (existing.Role == Roles.Admin)
            return Forbidden<User>("Admin cannot change the role of another admin");

        var roleResult = NormalizeRequired(role, DatabaseEnumMappings.UserRoles, "Invalid role");
        if (!roleResult.Succeeded) return Validation<User>(roleResult.Error!);
        var cinemaError = await ValidateCinemaAsync(roleResult.Value!, cinemaId, cancellationToken);
        if (cinemaError is not null) return Validation<User>(cinemaError);

        var log = BuildLog(adminId, userId, AdminActionTypes.ChangeRole,
            $"Changed role from {existing.Role} to {roleResult.Value}.", ipAddress);
        var user = await _repository.ChangeRoleAsync(
            userId, roleResult.Value!, cinemaId, log, cancellationToken);
        return user is null ? NotFound<User>() : AdminUserResult<User>.Success(user);
    }

    public async Task<AdminUserResult<User>> ChangeStatusAsync(
        int adminId, int userId, string status, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(userId, cancellationToken);
        if (existing is null) return NotFound<User>();
        var statusResult = NormalizeRequired(status, DatabaseEnumMappings.UserStatuses, "Invalid status");
        if (!statusResult.Succeeded) return Validation<User>(statusResult.Error!);
        if (adminId == userId && statusResult.Value is UserStatuses.Locked or UserStatuses.Inactive)
            return Forbidden<User>("Admin cannot lock or deactivate their own account");

        var log = BuildLog(adminId, userId, AdminActionTypes.AccountStatusChanged,
            $"Changed status from {existing.Status} to {statusResult.Value}.", ipAddress);
        var user = await _repository.ChangeStatusAsync(
            userId, statusResult.Value!, log, cancellationToken);
        return user is null ? NotFound<User>() : AdminUserResult<User>.Success(user);
    }

    public async Task<AdminUserResult<User>> ResetPasswordAsync(
        int adminId, int userId, string password, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidAdminPassword(password)) return Validation<User>(PasswordErrorMessage);
        if (await _repository.GetByIdAsync(userId, cancellationToken) is null) return NotFound<User>();
        var log = BuildLog(adminId, userId, AdminActionTypes.UpdateUser,
            "Reset password by administrator request.", ipAddress);
        var user = await _repository.ResetPasswordAsync(
            userId, PasswordHasher.Hash(password), log, cancellationToken);
        return user is null ? NotFound<User>() : AdminUserResult<User>.Success(user);
    }

    public async Task<AdminUserResult<User>> UploadAvatarAsync(
        int adminId, int userId, Stream imageStream, string fileName,
        string? contentType, long fileSize, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validationError = ImageFileValidator.Validate(fileName, contentType, fileSize);
        if (validationError is not null) return Validation<User>(validationError);
        var existing = await _repository.GetByIdAsync(userId, cancellationToken);
        if (existing is null) return NotFound<User>();
        if (!string.IsNullOrWhiteSpace(existing.AvatarURL)
            && string.IsNullOrWhiteSpace(existing.AvatarPublicId))
            return Validation<User>("Avatar cannot be replaced because its Cloudinary public ID is missing");

        var upload = await _imageStorageService.UploadImageAsync(
            imageStream, fileName, AvatarFolder, cancellationToken);
        var log = BuildLog(adminId, userId, AdminActionTypes.UpdateUser,
            "Updated avatar.", ipAddress);
        User? user;
        try
        {
            user = await _repository.UpdateAvatarAsync(
                userId, upload.SecureUrl, upload.PublicId, log, cancellationToken);
        }
        catch
        {
            await TryDeleteImageAsync(upload.PublicId, CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await TryDeleteImageAsync(upload.PublicId, cancellationToken);
            return NotFound<User>();
        }
        if (!string.IsNullOrWhiteSpace(existing.AvatarPublicId))
            await TryDeleteImageAsync(existing.AvatarPublicId, cancellationToken);
        return AdminUserResult<User>.Success(user);
    }

    public async Task<AdminUserResult<User>> DeleteAvatarAsync(
        int adminId, int userId, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(userId, cancellationToken);
        if (existing is null) return NotFound<User>();
        if (string.IsNullOrWhiteSpace(existing.AvatarURL)
            && string.IsNullOrWhiteSpace(existing.AvatarPublicId))
            return AdminUserResult<User>.Success(existing);
        if (string.IsNullOrWhiteSpace(existing.AvatarPublicId))
            return Validation<User>("Avatar cannot be deleted because its Cloudinary public ID is missing");

        await _imageStorageService.DeleteImageAsync(existing.AvatarPublicId, cancellationToken);
        var log = BuildLog(adminId, userId, AdminActionTypes.UpdateUser,
            "Deleted avatar.", ipAddress);
        var user = await _repository.UpdateAvatarAsync(userId, null, null, log, cancellationToken);
        return user is null ? NotFound<User>() : AdminUserResult<User>.Success(user);
    }

    public async Task<AdminUserResult<AdminUserDeleteResult>> DeleteAsync(
        int adminId, int userId, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (adminId == userId)
            return Forbidden<AdminUserDeleteResult>("Admin cannot delete their own account");
        var existing = await _repository.GetByIdAsync(userId, cancellationToken);
        if (existing is null) return NotFound<AdminUserDeleteResult>();

        if (!string.IsNullOrWhiteSpace(existing.AvatarURL)
            && string.IsNullOrWhiteSpace(existing.AvatarPublicId))
        {
            return AdminUserResult<AdminUserDeleteResult>.Failure(
                AdminUserErrorType.Storage,
                "Unable to delete the user's avatar because its Cloudinary public ID is missing. The user account was not deleted.");
        }

        if (await _repository.HasDeletionBlockingDataAsync(userId, cancellationToken))
            return await DeactivateAsync(adminId, userId, ipAddress,
                "Deactivated user because retained business or audit data prevents physical deletion.",
                cancellationToken);

        var deleteLog = BuildLog(adminId, null, AdminActionTypes.DeleteUser,
            "Permanently deleted user.", ipAddress);
        Exception? avatarDeleteException = null;
        bool physicallyDeleted;
        try
        {
            physicallyDeleted = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var deleted = await _repository.TryDeleteAsync(
                    userId, deleteLog, cancellationToken);
                if (!deleted) return false;

                if (!string.IsNullOrWhiteSpace(existing.AvatarPublicId))
                {
                    try
                    {
                        await _imageStorageService.DeleteImageAsync(
                            existing.AvatarPublicId, cancellationToken);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        avatarDeleteException = exception;
                        throw;
                    }
                }

                return true;
            }, cancellationToken);
        }
        catch (Exception) when (avatarDeleteException is not null)
        {
            _logger.LogError(
                avatarDeleteException,
                "Failed to delete avatar while deleting user {UserId}",
                userId);
            return AdminUserResult<AdminUserDeleteResult>.Failure(
                AdminUserErrorType.Storage,
                "Unable to delete the user's avatar. The user account was not deleted.");
        }

        if (!physicallyDeleted)
            return NotFound<AdminUserDeleteResult>();

        return AdminUserResult<AdminUserDeleteResult>.Success(new(true, false));
    }

    private async Task<AdminUserResult<AdminUserDeleteResult>> DeactivateAsync(
        int adminId, int userId, string? ipAddress, string description,
        CancellationToken cancellationToken)
    {
        var log = BuildLog(adminId, userId, AdminActionTypes.DeleteUser, description, ipAddress);
        var user = await _repository.ChangeStatusAsync(
            userId, UserStatuses.Inactive, log, cancellationToken);
        return user is null
            ? NotFound<AdminUserDeleteResult>()
            : AdminUserResult<AdminUserDeleteResult>.Success(new(false, true));
    }

    private async Task<string?> ValidateCinemaAsync(
        string role, int? cinemaId, CancellationToken cancellationToken)
    {
        if (role is Roles.Staff or Roles.Manager)
        {
            if (!cinemaId.HasValue) return "CinemaID is required for staff and manager";
            if (!await _repository.CinemaExistsAsync(cinemaId.Value, cancellationToken))
                return "Cinema not found";
            return null;
        }
        return cinemaId.HasValue ? "CinemaID must be null for customer and admin" : null;
    }

    private async Task TryDeleteImageAsync(string publicId, CancellationToken cancellationToken)
    {
        try
        {
            await _imageStorageService.DeleteImageAsync(publicId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Failed to clean up admin-managed avatar");
        }
    }

    private static AdminActionLog BuildLog(
        int adminId, int? targetUserId, string actionType,
        string description, string? ipAddress) => new()
    {
        AdminID = adminId,
        TargetUserID = targetUserId,
        TargetTable = "Users",
        TargetID = targetUserId,
        ActionType = actionType,
        Description = description,
        IPAddress = ipAddress,
        CreatedAt = DateTime.UtcNow
    };

    private static string? ValidateIdentityFields(string fullName, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Full name is required";
        if (fullName.Trim().Length > 100) return "Full name must not exceed 100 characters";
        if (string.IsNullOrWhiteSpace(email)) return "Email is required";
        if (email.Trim().Length > 150 || !new EmailAddressAttribute().IsValid(email.Trim()))
            return "Email is invalid";
        if (string.IsNullOrWhiteSpace(phone)) return "Phone is required";
        return Regex.IsMatch(phone.Trim(), @"^0[0-9]{9}$")
            ? null : "Phone number must contain 10 digits and start with 0";
    }

    private const string PasswordErrorMessage =
        "Password must contain at least 8 characters, 1 number, and 1 special character";
    private static bool IsValidAdminPassword(string password) =>
        !string.IsNullOrEmpty(password) && password.Length >= 8
        && Regex.IsMatch(password, @"\d") && Regex.IsMatch(password, "[^A-Za-z0-9]");

    private static (bool Succeeded, string? Value, string? Error) NormalizeRequired(
        string value, IReadOnlyDictionary<string, string> mappings, string error)
    {
        var result = EnumValueMapper.Validate(value, "Value", mappings);
        return result.Succeeded ? (true, result.DatabaseValue, null) : (false, null, error);
    }

    private static (bool Succeeded, string? Value, string? Error) NormalizeOptional(
        string? value, IReadOnlyDictionary<string, string> mappings, string error) =>
        string.IsNullOrWhiteSpace(value)
            ? (true, null, null)
            : NormalizeRequired(value, mappings, error);

    private static AdminUserResult<T> Validation<T>(string message) =>
        AdminUserResult<T>.Failure(AdminUserErrorType.Validation, message);
    private static AdminUserResult<T> Conflict<T>(string message) =>
        AdminUserResult<T>.Failure(AdminUserErrorType.Conflict, message);
    private static AdminUserResult<T> Forbidden<T>(string message) =>
        AdminUserResult<T>.Failure(AdminUserErrorType.Forbidden, message);
    private static AdminUserResult<T> NotFound<T>() =>
        AdminUserResult<T>.Failure(AdminUserErrorType.NotFound, "User not found");
}
