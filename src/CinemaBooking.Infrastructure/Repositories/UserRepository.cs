using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public UserRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetProfileByIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(user => user.LoyaltyTier)
            .Include(user => user.Cinema)
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserID == userId, cancellationToken);
    }

    public Task<User?> LookupCustomerAsync(
        string? email,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users
            .Include(user => user.LoyaltyTier)
            .Include(user => user.Wallet)
            .AsNoTracking()
            .Where(user => user.Role == Roles.Customer);

        return !string.IsNullOrWhiteSpace(email)
            ? users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken)
            : users.FirstOrDefaultAsync(user => user.Phone == phone, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(
        string phone,
        int? excludingUserId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(
            user => user.Phone == phone
                && (!excludingUserId.HasValue || user.UserID != excludingUserId.Value),
            cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(user => user.Cinema)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(u => u.LoyaltyTier)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
    }

    public async Task<User?> UpdateProfileAsync(
        int userId,
        string fullName,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.FullName = fullName;
        user.Phone = phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User?> UpdateAvatarAsync(
        int userId,
        string? avatarUrl,
        string? avatarPublicId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.AvatarURL = avatarUrl;
        user.AvatarPublicId = avatarPublicId;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserID == userId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserID == userId, cancellationToken);

        if (wallet is not null)
        {
            _dbContext.Wallets.Remove(wallet);
        }

        _dbContext.Users.Remove(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task AddUserWithWalletAsync(
        User user,
        Wallet wallet,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        wallet.UserID = user.UserID;
        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddUserWithWalletAndVerificationTokenAsync(
        User user,
        Wallet wallet,
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        wallet.UserID = user.UserID;
        verificationToken.UserID = user.UserID;

        _dbContext.Wallets.Add(wallet);
        _dbContext.EmailVerificationTokens.Add(verificationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public Task AddEmailVerificationTokenAsync(
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default)
    {
        _dbContext.EmailVerificationTokens.Add(verificationToken);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public Task<EmailVerificationToken?> GetLatestEmailVerificationTokenAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EmailVerificationTokens
            .AsNoTracking()
            .Where(t => t.UserID == userId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddPasswordResetTokenAsync(
        PasswordResetToken resetToken,
        CancellationToken cancellationToken = default)
    {
        _dbContext.PasswordResetTokens.Add(resetToken);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<PasswordResetToken?> GetLatestPasswordResetTokenAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PasswordResetTokens
            .AsNoTracking()
            .Where(t => t.UserID == userId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ReplaceUnusedPasswordResetTokensAsync(
        int userId,
        PasswordResetToken resetToken,
        CancellationToken cancellationToken = default)
    {
        var unusedTokens = await _dbContext.PasswordResetTokens
            .Where(t => t.UserID == userId && !t.UsedAt.HasValue)
            .ToListAsync(cancellationToken);

        _dbContext.PasswordResetTokens.RemoveRange(unusedTokens);
        _dbContext.PasswordResetTokens.Add(resetToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceUnverifiedEmailVerificationTokensAsync(
        int userId,
        EmailVerificationToken verificationToken,
        CancellationToken cancellationToken = default)
    {
        var unverifiedTokens = await _dbContext.EmailVerificationTokens
            .Where(t => t.UserID == userId && !t.VerifiedAt.HasValue)
            .ToListAsync(cancellationToken);

        _dbContext.EmailVerificationTokens.RemoveRange(unverifiedTokens);
        _dbContext.EmailVerificationTokens.Add(verificationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var verificationToken = await _dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (verificationToken is null)
        {
            return;
        }

        _dbContext.EmailVerificationTokens.Remove(verificationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PasswordResetTokens
            .Where(t => t.Token == token)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> TryResetPasswordAsync(
        string token,
        string passwordHash,
        DateTime resetAt,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var userId = await _dbContext.PasswordResetTokens
            .AsNoTracking()
            .Where(t => t.Token == token)
            .Select(t => (int?)t.UserID)
            .SingleOrDefaultAsync(cancellationToken);

        if (!userId.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var consumedTokenCount = await _dbContext.PasswordResetTokens
            .Where(t => t.Token == token
                && !t.UsedAt.HasValue
                && t.ExpiresAt > resetAt)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.UsedAt, resetAt),
                cancellationToken);

        if (consumedTokenCount == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var updatedUserCount = await _dbContext.Users
            .Where(u => u.UserID == userId.Value)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.PasswordHash, passwordHash)
                    .SetProperty(u => u.UpdatedAt, resetAt)
                    .SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1),
                cancellationToken);

        if (updatedUserCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryIncrementTokenVersionAsync(
        int userId,
        int expectedTokenVersion,
        CancellationToken cancellationToken = default)
    {
        var updatedUserCount = await _dbContext.Users
            .Where(u => u.UserID == userId && u.TokenVersion == expectedTokenVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        return updatedUserCount == 1;
    }

    public async Task<bool> TryUpdatePasswordHashAsync(
        int userId,
        string expectedPasswordHash,
        string newPasswordHash,
        CancellationToken cancellationToken = default)
    {
        var updatedUserCount = await _dbContext.Users
            .Where(u => u.UserID == userId && u.PasswordHash == expectedPasswordHash)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.PasswordHash, newPasswordHash)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        return updatedUserCount == 1;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
