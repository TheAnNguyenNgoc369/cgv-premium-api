using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IVoucherRepository
{
    Task<(List<Voucher> Items, int Total)> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<Voucher?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, int? excludingId, CancellationToken cancellationToken);
    Task<bool> HasTransactionsAsync(int id, CancellationToken cancellationToken);
    Task<Voucher> AddAsync(Voucher voucher, AdminActionLog log, CancellationToken cancellationToken);
    Task<Voucher?> UpdateAsync(Voucher voucher, AdminActionLog log, CancellationToken cancellationToken);
    Task<bool> DeactivateAsync(int id, AdminActionLog log, CancellationToken cancellationToken);
    Task<List<Voucher>> GetRedeemableVouchersAsync(CancellationToken cancellationToken);
    Task<Voucher?> GetForRedemptionAsync(int voucherId, CancellationToken cancellationToken);
    Task<int> GetUserRedemptionCountAsync(int userId, int voucherId, CancellationToken cancellationToken);
}
