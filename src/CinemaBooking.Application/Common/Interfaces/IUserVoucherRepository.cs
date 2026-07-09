using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IUserVoucherRepository
{
    Task<List<UserVoucher>> GetUserVouchersAsync(int userId, CancellationToken cancellationToken);
    Task<UserVoucher?> GetByIdAsync(int userVoucherId, CancellationToken cancellationToken);
    Task RedeemVoucherAsync(
        UserVoucher userVoucher,
        LoyaltyPoints loyaltyPoint,
        int pointsToDeduct,
        AdminActionLog log,
        CancellationToken cancellationToken);
}
