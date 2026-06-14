using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public UserRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(u => u.Phone == phone, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
    }

    public Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserID == userId, cancellationToken);
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

    public Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
