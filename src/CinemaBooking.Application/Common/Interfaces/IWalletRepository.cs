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
}
