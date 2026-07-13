using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.LoyaltyTiers;

public interface ILoyaltyTierService
{
    Task<List<LoyaltyTier>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<LoyaltyTier?> GetByIdAsync(int tierId, CancellationToken cancellationToken = default);

    Task<LoyaltyTierResult> CreateAsync(
        string? tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken = default);

    Task<LoyaltyTierResult> UpdateAsync(
        int tierId,
        string? tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken = default);

    Task<LoyaltyTierDeleteResult> DeleteAsync(
        int tierId,
        CancellationToken cancellationToken = default);
}
