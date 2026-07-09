using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.AdminUsers;

public interface IAdminUserService
{
    Task<AdminUserResult<AdminUserPageResult>> GetUsersAsync(
        string? search, string? role, string? status, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<User>> CreateAsync(
        int adminId, AdminUserCreateCommand command, string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<User>> UpdateAsync(
        int adminId, int userId, AdminUserUpdateCommand command, string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<User>> ChangeRoleAsync(
        int adminId, int userId, string role, int? cinemaId, string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<User>> ChangeStatusAsync(
        int adminId, int userId, string status, string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<User>> ResetPasswordAsync(
        int adminId, int userId, string password, string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<User>> UploadAvatarAsync(
        int adminId, int userId, Stream imageStream, string fileName,
        string? contentType, long fileSize, string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<User>> DeleteAvatarAsync(
        int adminId, int userId, string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AdminUserResult<AdminUserDeleteResult>> DeleteAsync(
        int adminId, int userId, string? ipAddress,
        CancellationToken cancellationToken = default);
}
