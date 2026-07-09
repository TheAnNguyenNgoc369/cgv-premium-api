using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IAdminUserRepository
{
    Task<(IReadOnlyList<User> Items, int TotalItems)> GetPageAsync(
        string? search, string? role, string? status, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, int? excludingUserId = null,
        CancellationToken cancellationToken = default);
    Task<bool> PhoneExistsAsync(string phone, int? excludingUserId = null,
        CancellationToken cancellationToken = default);
    Task<bool> CinemaExistsAsync(int cinemaId, CancellationToken cancellationToken = default);
    Task<bool> HasDeletionBlockingDataAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(User user, Wallet wallet, AdminActionLog actionLog,
        CancellationToken cancellationToken = default);
    Task<User?> UpdateAsync(int userId, string fullName, string email, string phone,
        int? cinemaId, AdminActionLog actionLog, CancellationToken cancellationToken = default);
    Task<User?> ChangeRoleAsync(int userId, string role, int? cinemaId,
        AdminActionLog actionLog, CancellationToken cancellationToken = default);
    Task<User?> ChangeStatusAsync(int userId, string status, AdminActionLog actionLog,
        CancellationToken cancellationToken = default);
    Task<User?> ResetPasswordAsync(int userId, string passwordHash, AdminActionLog actionLog,
        CancellationToken cancellationToken = default);
    Task<User?> UpdateAvatarAsync(int userId, string? avatarUrl, string? avatarPublicId,
        AdminActionLog actionLog, CancellationToken cancellationToken = default);
    Task<bool> TryDeleteAsync(int userId, AdminActionLog actionLog,
        CancellationToken cancellationToken = default);
}
