using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public AdminUserRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPageAsync(
        string? search, string? role, string? status, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .Include(user => user.LoyaltyTier)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (int.TryParse(search, out var userId))
            {
                query = query.Where(user => user.UserID == userId);
            }
            else
            {
                query = query.Where(user =>
                    EF.Functions.Like(user.FullName, $"%{search}%")
                    || EF.Functions.Like(user.Email, $"%{search}%"));
            }
        }

        if (role is not null) query = query.Where(user => user.Role == role);
        if (status is not null) query = query.Where(user => user.Status == status);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(user => user.UserID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<User?> GetByIdAsync(
        int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserID == userId, cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email, int? excludingUserId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(user =>
            user.Email == email
            && (!excludingUserId.HasValue || user.UserID != excludingUserId.Value),
            cancellationToken);

    public Task<bool> PhoneExistsAsync(
        string phone, int? excludingUserId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(user =>
            user.Phone == phone
            && (!excludingUserId.HasValue || user.UserID != excludingUserId.Value),
            cancellationToken);

    public Task<bool> CinemaExistsAsync(
        int cinemaId, CancellationToken cancellationToken = default) =>
        _dbContext.Cinemas.AnyAsync(cinema => cinema.CinemaID == cinemaId, cancellationToken);

    public async Task<bool> HasDeletionBlockingDataAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Bookings.AnyAsync(
                booking => booking.UserID == userId || booking.CreatedByStaffID == userId,
                cancellationToken))
            return true;

        if (await _dbContext.Wallets.AnyAsync(
                wallet => wallet.UserID == userId
                    && (wallet.Balance != 0 || wallet.Transactions.Any()),
                cancellationToken))
            return true;

        if (await _dbContext.LoyaltyPoints.AnyAsync(
                points => points.UserID == userId, cancellationToken))
            return true;

        if (await _dbContext.AdminActionLogs.AnyAsync(
                log => log.AdminID == userId, cancellationToken))
            return true;

        if (await _dbContext.EmailLogs.AnyAsync(
                log => log.UserID == userId, cancellationToken))
            return true;

        if (await _dbContext.Refunds.AnyAsync(
                refund => refund.ProcessedBy == userId, cancellationToken))
            return true;

        return await _dbContext.Tickets.AnyAsync(
            ticket => ticket.CheckedInByID == userId, cancellationToken);
    }

    public async Task AddAsync(
        User user, Wallet wallet, AdminActionLog actionLog,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        wallet.UserID = user.UserID;
        actionLog.TargetUserID = user.UserID;
        actionLog.TargetID = user.UserID;
        _dbContext.Wallets.Add(wallet);
        _dbContext.AdminActionLogs.Add(actionLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public Task<User?> UpdateAsync(
        int userId, string fullName, string email, string phone, int? cinemaId,
        AdminActionLog actionLog, CancellationToken cancellationToken = default) =>
        UpdateWithLogAsync(userId, actionLog, user =>
        {
            var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.CinemaID = cinemaId;
            if (emailChanged)
            {
                user.EmailVerifiedAt = DateTime.UtcNow;
                user.TokenVersion++;
            }
        }, cancellationToken);

    public Task<User?> ChangeRoleAsync(
        int userId, string role, int? cinemaId, AdminActionLog actionLog,
        CancellationToken cancellationToken = default) =>
        UpdateWithLogAsync(userId, actionLog, user =>
        {
            user.Role = role;
            user.CinemaID = cinemaId;
            user.TokenVersion++;
        }, cancellationToken);

    public Task<User?> ChangeStatusAsync(
        int userId, string status, AdminActionLog actionLog,
        CancellationToken cancellationToken = default) =>
        UpdateWithLogAsync(userId, actionLog, user =>
        {
            user.Status = status;
            if (status == UserStatuses.Unverified)
            {
                user.EmailVerifiedAt = null;
            }
            else if (status == UserStatuses.Active && !user.EmailVerifiedAt.HasValue)
            {
                user.EmailVerifiedAt = DateTime.UtcNow;
            }
            user.TokenVersion++;
        }, cancellationToken);

    public Task<User?> ResetPasswordAsync(
        int userId, string passwordHash, AdminActionLog actionLog,
        CancellationToken cancellationToken = default) =>
        UpdateWithLogAsync(userId, actionLog, user =>
        {
            user.PasswordHash = passwordHash;
            user.TokenVersion++;
        }, cancellationToken);

    public Task<User?> UpdateAvatarAsync(
        int userId, string? avatarUrl, string? avatarPublicId, AdminActionLog actionLog,
        CancellationToken cancellationToken = default) =>
        UpdateWithLogAsync(userId, actionLog, user =>
        {
            user.AvatarURL = avatarUrl;
            user.AvatarPublicId = avatarPublicId;
        }, cancellationToken);

    public async Task<bool> TryDeleteAsync(
        int userId, AdminActionLog actionLog,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.UserID == userId, cancellationToken);
        if (user is null) return false;

        await _dbContext.EmailVerificationTokens
            .Where(token => token.UserID == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.PasswordResetTokens
            .Where(token => token.UserID == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Notifications
            .Where(notification => notification.UserID == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SeatHolds
            .Where(hold => hold.UserID == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(
            candidate => candidate.UserID == userId, cancellationToken);
        if (wallet is not null) _dbContext.Wallets.Remove(wallet);

        var existingLogs = await _dbContext.AdminActionLogs
            .Where(log => log.TargetUserID == userId)
            .ToListAsync(cancellationToken);
        foreach (var log in existingLogs)
        {
            log.TargetUserID = null;
            log.TargetTable ??= "Users";
            log.TargetID ??= userId;
        }

        _dbContext.Users.Remove(user);
        actionLog.TargetUserID = null;
        actionLog.TargetID = userId;
        _dbContext.AdminActionLogs.Add(actionLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<User?> UpdateWithLogAsync(
        int userId, AdminActionLog actionLog, Action<User> update,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.UserID == userId, cancellationToken);
        if (user is null) return null;

        update(user);
        user.UpdatedAt = DateTime.UtcNow;
        actionLog.TargetUserID = userId;
        actionLog.TargetID = userId;
        _dbContext.AdminActionLogs.Add(actionLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user;
    }
}
