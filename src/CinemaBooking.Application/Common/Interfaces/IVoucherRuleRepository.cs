using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IVoucherRuleRepository
{
    Task<List<VoucherRule>> GetByVoucherIdAsync(int voucherId, CancellationToken cancellationToken);
    Task<VoucherRule?> GetByIdAsync(int ruleId, CancellationToken cancellationToken);
    Task<VoucherRule> AddAsync(VoucherRule rule, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int ruleId, CancellationToken cancellationToken);
    Task<List<VoucherRule>> GetByRuleTypeAsync(int voucherId, string ruleType, CancellationToken cancellationToken);
}
