using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class UserVoucherRepository : IUserVoucherRepository
{
    private readonly CinemaBookingDbContext _db;

    public UserVoucherRepository(CinemaBookingDbContext db) => _db = db;

    public async Task<List<UserVoucher>> GetUserVouchersAsync(int userId, CancellationToken ct)
    {
        return await _db.Set<UserVoucher>()
            .Include(uv => uv.Voucher)
            .Where(uv => uv.UserID == userId)
            .OrderByDescending(uv => uv.RedeemedAt)
            .ToListAsync(ct);
    }

    public Task<UserVoucher?> GetByIdAsync(int userVoucherId, CancellationToken ct) =>
        _db.Set<UserVoucher>()
            .Include(uv => uv.Voucher)
            .FirstOrDefaultAsync(uv => uv.UserVoucherID == userVoucherId, ct);

    public async Task RedeemVoucherAsync(
        UserVoucher userVoucher,
        LoyaltyPoints loyaltyPoint,
        int pointsToDeduct,
        AdminActionLog log,
        CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userVoucher.UserID, ct);
            if (user is null) throw new InvalidOperationException("User not found");

            var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherID == userVoucher.VoucherID, ct);
            if (voucher is null) throw new InvalidOperationException("Voucher not found");

            user.TotalPoints -= pointsToDeduct;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.Set<UserVoucher>().AddAsync(userVoucher, ct);
            await _db.LoyaltyPoints.AddAsync(loyaltyPoint, ct);
            _db.AdminActionLogs.Add(log);

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }
}
