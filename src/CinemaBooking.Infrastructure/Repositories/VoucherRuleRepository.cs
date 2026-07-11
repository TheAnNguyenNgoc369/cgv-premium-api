using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class VoucherRuleRepository : IVoucherRuleRepository
{
    private readonly CinemaBookingDbContext _db;

    public VoucherRuleRepository(CinemaBookingDbContext db) => _db = db;

    public async Task<List<VoucherRule>> GetByVoucherIdAsync(int voucherId, CancellationToken ct) =>
        await _db.Set<VoucherRule>()
            .AsNoTracking()
            .Where(vr => vr.VoucherID == voucherId)
            .OrderBy(vr => vr.RuleType)
            .ToListAsync(ct);

    public Task<VoucherRule?> GetByIdAsync(int ruleId, CancellationToken ct) =>
        _db.Set<VoucherRule>().FirstOrDefaultAsync(vr => vr.RuleID == ruleId, ct);

    public async Task<VoucherRule> AddAsync(VoucherRule rule, CancellationToken ct)
    {
        _db.Set<VoucherRule>().Add(rule);
        await _db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<bool> DeleteAsync(int ruleId, CancellationToken ct)
    {
        var rule = await _db.Set<VoucherRule>().FindAsync([ruleId], ct);
        if (rule is null) return false;
        _db.Set<VoucherRule>().Remove(rule);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<VoucherRule>> GetByRuleTypeAsync(int voucherId, string ruleType, CancellationToken ct) =>
        await _db.Set<VoucherRule>()
            .AsNoTracking()
            .Where(vr => vr.VoucherID == voucherId && vr.RuleType == ruleType)
            .ToListAsync(ct);
}
