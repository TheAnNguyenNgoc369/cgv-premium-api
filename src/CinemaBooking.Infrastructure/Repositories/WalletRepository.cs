using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly CinemaBookingDbContext _db;

    public WalletRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Wallet?> GetWalletByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Wallets
            .FirstOrDefaultAsync(w => w.UserID == userId, cancellationToken);
    }

    public async Task<bool> CheckSufficientBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        return wallet is not null && wallet.Balance >= amount;
    }

    public async Task DeductBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.UserID == userId, cancellationToken);

        if (wallet is null)
            throw new InvalidOperationException($"Wallet not found for user {userId}");

        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        wallet.Balance -= amount;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.UserID == userId, cancellationToken);

        if (wallet is null)
            throw new InvalidOperationException($"Wallet not found for user {userId}");

        wallet.Balance += amount;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WalletTransaction> CreateTransactionAsync(
        WalletTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await _db.WalletTransactions.AddAsync(transaction, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<List<WalletTransaction>> GetTransactionHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
            return new List<WalletTransaction>();

        return await _db.WalletTransactions
            .Where(t => t.WalletID == wallet.WalletID)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
