using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.LoyaltyTiers;

public sealed class LoyaltyTierService(ILoyaltyRepository repository) : ILoyaltyTierService
{
    public Task<List<LoyaltyTier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetAllTiersAsync(cancellationToken);
    }

    public Task<LoyaltyTier?> GetByIdAsync(
        int tierId,
        CancellationToken cancellationToken = default)
    {
        return repository.GetTierByIdAsync(tierId, cancellationToken);
    }

    public Task<LoyaltyTierResult> CreateAsync(
        string? tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(
            null,
            tierName,
            minPoints,
            discountRate,
            maxRefundPerMonth,
            cancellationToken);
    }

    public async Task<LoyaltyTierResult> UpdateAsync(
        int tierId,
        string? tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetTierByIdAsync(tierId, cancellationToken) is null)
        {
            return new(false, "Loyalty tier not found.", null, IsNotFound: true);
        }

        return await SaveAsync(
            tierId,
            tierName,
            minPoints,
            discountRate,
            maxRefundPerMonth,
            cancellationToken);
    }

    public async Task<LoyaltyTierDeleteResult> DeleteAsync(
        int tierId,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetTierByIdAsync(tierId, cancellationToken) is null)
        {
            return new(false, "Loyalty tier not found.", IsNotFound: true);
        }

        if (await repository.HasAssignedUsersAsync(tierId, cancellationToken))
        {
            return new(false, "Loyalty tier is currently assigned to one or more users.", IsConflict: true);
        }

        return await repository.DeleteTierAsync(tierId, cancellationToken)
            ? new(true, null)
            : new(false, "Loyalty tier not found.", IsNotFound: true);
    }

    private async Task<LoyaltyTierResult> SaveAsync(
        int? tierId,
        string? tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(tierName);
        var validationError = Validate(normalizedName, minPoints, discountRate, maxRefundPerMonth);

        if (validationError is not null)
        {
            return new(false, validationError, null);
        }

        if (await repository.TierNameExistsAsync(normalizedName!, tierId, cancellationToken))
        {
            return new(false, "Loyalty tier name must be unique.", null, IsConflict: true);
        }

        if (await repository.MinPointsExistsAsync(minPoints, tierId, cancellationToken))
        {
            return new(false, "Min points must be unique.", null, IsConflict: true);
        }

        if (tierId.HasValue)
        {
            var updated = await repository.UpdateTierAsync(
                tierId.Value,
                normalizedName!,
                minPoints,
                discountRate,
                maxRefundPerMonth,
                cancellationToken);

            return updated is null
                ? new(false, "Loyalty tier not found.", null, IsNotFound: true)
                : new(true, null, updated);
        }

        var tier = new LoyaltyTier
        {
            TierName = normalizedName!,
            MinPoints = minPoints,
            DiscountRate = discountRate,
            MaxRefundPerMonth = maxRefundPerMonth
        };

        return new(true, null, await repository.AddTierAsync(tier, cancellationToken));
    }

    private static string? NormalizeName(string? tierName)
    {
        return string.IsNullOrWhiteSpace(tierName)
            ? null
            : tierName.Trim();
    }

    private static string? Validate(
        string? tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth)
    {
        if (tierName is null)
        {
            return "tierName is required.";
        }

        if (tierName.Length > 20)
        {
            return "tierName must not exceed 20 characters.";
        }

        if (minPoints < 0)
        {
            return "minPoints must be greater than or equal to 0.";
        }

        if (discountRate < 0 || discountRate > 1)
        {
            return "discountRate must be between 0 and 1.";
        }

        return maxRefundPerMonth < 0
            ? "maxRefundPerMonth must be greater than or equal to 0."
            : null;
    }
}
