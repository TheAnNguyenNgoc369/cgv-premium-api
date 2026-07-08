using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetWalletByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> CheckSufficientBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task DeductBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task AddBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<WalletTransaction> CreateTransactionAsync(
        WalletTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<List<WalletTransaction>> GetTransactionHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<WalletTransaction?> GetTransactionByIdAsync(
        int transactionId,
        CancellationToken cancellationToken = default);

    Task<(List<WalletTransaction> Transactions, int TotalCount)> GetTransactionsWithFiltersAsync(
        int userId,
        int page,
        int pageSize,
        DateTime? fromDate,
        DateTime? toDate,
        string? transactionType,
        CancellationToken cancellationToken = default);

    Task<(decimal TotalRefundReceived, decimal TotalSpent, int TransactionCount)> GetWalletSummaryAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
