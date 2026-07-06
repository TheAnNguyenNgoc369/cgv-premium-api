using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class VoucherRepository : IVoucherRepository
{
    private readonly CinemaBookingDbContext _db;
    public VoucherRepository(CinemaBookingDbContext db) => _db = db;

    public async Task<(List<Voucher> Items, int Total)> GetPageAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Vouchers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(v => v.VoucherCode.Contains(search)
            || (v.Description != null && v.Description.Contains(search)) || (v.Category != null && v.Category.Contains(search)));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(v => v.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
    public Task<Voucher?> GetByIdAsync(int id, CancellationToken ct) => _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherID == id, ct);
    public Task<bool> CodeExistsAsync(string code, int? excludingId, CancellationToken ct) =>
        _db.Vouchers.AnyAsync(v => v.VoucherCode == code && (!excludingId.HasValue || v.VoucherID != excludingId), ct);
    public Task<bool> HasTransactionsAsync(int id, CancellationToken ct) => _db.BookingVouchers.AnyAsync(v => v.VoucherID == id, ct);
    public async Task<Voucher> AddAsync(Voucher voucher, AdminActionLog log, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(ct);
        log.TargetID = voucher.VoucherID;
        _db.AdminActionLogs.Add(log);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return voucher;
    }
    public async Task<Voucher?> UpdateAsync(Voucher voucher, AdminActionLog log, CancellationToken ct)
    { _db.AdminActionLogs.Add(log); await _db.SaveChangesAsync(ct); return voucher; }
    public async Task<bool> DeactivateAsync(int id, AdminActionLog log, CancellationToken ct)
    { var voucher = await _db.Vouchers.FindAsync([id], ct); if (voucher is null) return false; voucher.IsActive = false; _db.AdminActionLogs.Add(log); await _db.SaveChangesAsync(ct); return true; }

    public async Task<List<Voucher>> GetRedeemableVouchersAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _db.Vouchers
            .AsNoTracking()
            .Where(v => v.IsActive
                && v.IsRedeemable
                && v.RequiredPoints.HasValue
                && v.ValidFrom <= now
                && v.ValidUntil >= now
                && (!v.RemainingQuantity.HasValue || v.RemainingQuantity > 0))
            .OrderBy(v => v.RequiredPoints)
            .ToListAsync(ct);
    }

    public Task<Voucher?> GetForRedemptionAsync(int voucherId, CancellationToken ct) =>
        _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherID == voucherId, ct);

    public async Task<int> GetUserRedemptionCountAsync(int userId, int voucherId, CancellationToken ct) =>
        await _db.Set<UserVoucher>().CountAsync(uv => uv.UserID == userId && uv.VoucherID == voucherId, ct);
}
