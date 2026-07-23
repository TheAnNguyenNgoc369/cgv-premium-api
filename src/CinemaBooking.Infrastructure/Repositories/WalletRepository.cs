using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
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
        var affectedRows = await _db.Wallets
            .Where(wallet => wallet.UserID == userId && wallet.Balance >= amount)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    wallet => wallet.Balance,
                    wallet => wallet.Balance - amount),
                cancellationToken);

        if (affectedRows == 0)
            throw new InvalidOperationException("Insufficient balance or wallet not found");
    }

    public async Task<bool> TryDeductBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _db.Wallets
            .Where(wallet => wallet.UserID == userId && wallet.Balance >= amount)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    wallet => wallet.Balance,
                    wallet => wallet.Balance - amount),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task AddBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _db.Wallets
            .Where(wallet => wallet.UserID == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    wallet => wallet.Balance,
                    wallet => wallet.Balance + amount),
                cancellationToken);

        if (affectedRows == 0)
            throw new InvalidOperationException($"Wallet not found for user {userId}");
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

    public async Task<WalletTransaction?> GetTransactionByIdAsync(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        return await _db.WalletTransactions
            .Include(t => t.Booking)
            .FirstOrDefaultAsync(t => t.TransactionID == transactionId, cancellationToken);
    }

    public async Task<(List<WalletTransaction> Transactions, int TotalCount)> GetTransactionsWithFiltersAsync(
        int userId,
        int page,
        int pageSize,
        DateTime? fromDate,
        DateTime? toDate,
        string? transactionType,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
            return (new List<WalletTransaction>(), 0);

        var query = _db.WalletTransactions
            .Include(t => t.Booking)
            .Where(t => t.WalletID == wallet.WalletID);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(transactionType))
            query = query.Where(t => t.TransactionType == transactionType);

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (transactions, totalCount);
    }

    public async Task<(decimal TotalRefundReceived, decimal TotalSpent, int TransactionCount)> GetWalletSummaryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
            return (0, 0, 0);

        var query = _db.WalletTransactions.Where(t => t.WalletID == wallet.WalletID);

        var totalRefundReceived = await query
            .Where(t => t.TransactionType == WalletTransactionType.Refund)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0;

        var paymentSum = await query
            .Where(t => t.TransactionType == WalletTransactionType.Payment)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0;
        var totalSpent = -paymentSum;

        var transactionCount = await query.CountAsync(cancellationToken);

        return (totalRefundReceived, totalSpent, transactionCount);
    }
}
