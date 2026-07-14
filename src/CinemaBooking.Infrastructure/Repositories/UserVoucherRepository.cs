using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
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

    public Task<UserVoucher?> GetAvailableOwnedAsync(int userId, int voucherId, DateTime now, CancellationToken ct) =>
        _db.Set<UserVoucher>()
            .AsNoTracking()
            .Where(uv => uv.UserID == userId
                && uv.VoucherID == voucherId
                && uv.Status == UserVoucherStatus.Available
                && uv.BookingID == null
                && uv.ExpiredAt >= now)
            .OrderBy(uv => uv.ExpiredAt)
            .FirstOrDefaultAsync(ct);

    public async Task<UserVoucher?> GetAvailableForUpdateAsync(int userId, int voucherId, CancellationToken ct) =>
        await _db.Set<UserVoucher>()
            .FromSqlRaw(
                "SELECT * FROM UserVoucher WITH (UPDLOCK, ROWLOCK) WHERE UserID = {0} AND VoucherID = {1} AND Status = {2} AND BookingID IS NULL",
                userId, voucherId, UserVoucherStatus.Available)
            .OrderBy(uv => uv.ExpiredAt)
            .FirstOrDefaultAsync(ct);

    // A UserVoucher is "reserved for a booking" when BookingID is set and Status is still Available.
    // The Status column has no separate Reserved value (DB CHECK constraint disallows it).
    public async Task MarkReservedAsUsedByBookingAsync(int bookingId, DateTime usedAt, CancellationToken ct) =>
        await _db.Set<UserVoucher>()
            .Where(uv => uv.BookingID == bookingId && uv.Status == UserVoucherStatus.Available)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(uv => uv.Status, UserVoucherStatus.Used)
                .SetProperty(uv => uv.UsedAt, usedAt), ct);

    public async Task ReleaseReservedByBookingAsync(int bookingId, CancellationToken ct) =>
        await _db.Set<UserVoucher>()
            .Where(uv => uv.BookingID == bookingId && uv.Status == UserVoucherStatus.Available)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(uv => uv.BookingID, (int?)null), ct);

    public async Task<(bool Succeeded, string? Error)> RedeemVoucherAsync(
        UserVoucher userVoucher,
        LoyaltyPoints loyaltyPoint,
        int pointsToDeduct,
        int? maxUses,
        int? exchangeLimit,
        AdminActionLog log,
        CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            // Lock the user row so concurrent redeems serialize on point deduction / limit checks.
            var user = await _db.Users
                .FromSqlRaw("SELECT * FROM Users WITH (UPDLOCK, ROWLOCK) WHERE UserID = {0}", userVoucher.UserID)
                .FirstOrDefaultAsync(ct);
            if (user is null) return (false, "User not found");

            // Lock the voucher row: the global UsedCount is bumped here, so concurrent
            // redeems must serialize on this row for the MaxUses gate to be exact.
            var voucher = await _db.Vouchers
                .FromSqlRaw("SELECT * FROM Voucher WITH (UPDLOCK, ROWLOCK) WHERE VoucherID = {0}", userVoucher.VoucherID)
                .FirstOrDefaultAsync(ct);
            if (voucher is null) return (false, "Voucher not found");

            // Re-validate voucher state inside the transaction (it may have changed since the pre-check).
            var now = DateTime.UtcNow;
            if (!voucher.IsActive) return (false, "Voucher is not active");
            if (now < voucher.ValidFrom) return (false, "Voucher is not yet valid");
            if (now > voucher.ValidUntil) return (false, "Voucher has expired");

            // Re-check the global redemption cap against the locked counter.
            if (maxUses.HasValue && voucher.UsedCount >= maxUses.Value)
                return (false, "Voucher redemption limit has been reached.");

            // Re-check point sufficiency against the locked balance: points must never go negative.
            if (user.TotalPoints < pointsToDeduct)
                return (false, $"Insufficient points. Required: {pointsToDeduct}, Available: {user.TotalPoints}");

            // Re-check exchange limit against the committed count: limit must never be exceeded.
            if (exchangeLimit.HasValue)
            {
                var redemptionCount = await _db.Set<UserVoucher>()
                    .CountAsync(uv => uv.UserID == userVoucher.UserID && uv.VoucherID == userVoucher.VoucherID, ct);
                if (redemptionCount >= exchangeLimit.Value)
                    return (false, $"Exchange limit reached. Maximum {exchangeLimit.Value} redemptions per user");
            }

            user.TotalPoints -= pointsToDeduct;
            user.UpdatedAt = now;

            // Loyalty: bump the global counter at redeem, NOT at booking.
            voucher.UsedCount++;

            await _db.Set<UserVoucher>().AddAsync(userVoucher, ct);
            await _db.LoyaltyPoints.AddAsync(loyaltyPoint, ct);
            _db.AdminActionLogs.Add(log);

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return (true, (string?)null);
        });
    }
}
