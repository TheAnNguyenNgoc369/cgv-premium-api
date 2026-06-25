using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Membership;

public sealed class MembershipService : IMembershipService
{
    private readonly ILoyaltyRepository _loyaltyRepository;

    public MembershipService(ILoyaltyRepository loyaltyRepository)
    {
        _loyaltyRepository = loyaltyRepository;
    }

    public async Task<MembershipInfo> GetMyMembershipAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var totalPoints = await _loyaltyRepository.GetUserTotalPointsAsync(userId, cancellationToken);
        var totalSpent = await _loyaltyRepository.GetUserTotalSpentAsync(userId, cancellationToken);
        var tier = await _loyaltyRepository.GetUserTierAsync(userId, cancellationToken);

        var tierName = tier?.TierName ?? MembershipTiers.Member;
        var discountRate = tier?.DiscountRate ?? MembershipTiers.MemberDiscountRate;
        var discountPercent = discountRate * 100;

        var isVip = string.Equals(tierName, MembershipTiers.VIP, StringComparison.OrdinalIgnoreCase);

        var pointsToVip = 0;
        if (!isVip)
        {
            var pointsNeeded = MembershipTiers.VipMinPoints - totalPoints;
            pointsToVip = pointsNeeded > 0 ? pointsNeeded : 0;
        }

        return new MembershipInfo(
            Tier: tierName,
            TotalPoints: totalPoints,
            TotalSpent: totalSpent,
            DiscountPercent: discountPercent,
            IsVip: isVip,
            PointsToVip: pointsToVip
        );
    }

    public async Task<List<LoyaltyTier>> GetTiersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _loyaltyRepository.GetAllTiersAsync(cancellationToken);
    }

    public async Task<List<LoyaltyPointHistory>> GetPointHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var points = await _loyaltyRepository.GetPointHistoryAsync(userId, cancellationToken);

        return points.Select(p => new LoyaltyPointHistory(
            PointsDelta: p.PointsDelta,
            TransactionType: p.TransactionType,
            Description: p.Description,
            CreatedAt: p.CreatedAt
        )).ToList();
    }

    public async Task AddPointsAfterPaymentSuccessAsync(
        int userId,
        int bookingId,
        decimal finalAmount,
        CancellationToken cancellationToken = default)
    {
        var pointsEarned = (int)(finalAmount * MembershipTiers.PointsPerVnd);

        if (pointsEarned <= 0)
            return;

        var loyaltyPoint = new LoyaltyPoints
        {
            UserID = userId,
            BookingID = bookingId,
            PointsDelta = pointsEarned,
            TransactionType = LoyaltyTransactionTypes.Earned,
            Description = $"Earned {pointsEarned} points from booking payment of {finalAmount:N0} VND",
            CreatedAt = DateTime.UtcNow
        };

        await _loyaltyRepository.AddLoyaltyPointAsync(loyaltyPoint, cancellationToken);

        var currentTotalPoints = await _loyaltyRepository.GetUserTotalPointsAsync(userId, cancellationToken);
        var newTotalPoints = currentTotalPoints + pointsEarned;

        await _loyaltyRepository.UpdateUserTotalPointsAsync(userId, newTotalPoints, cancellationToken);

        await CheckAndUpgradeTierAsync(userId, cancellationToken);
    }

    public async Task CheckAndUpgradeTierAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var totalPoints = await _loyaltyRepository.GetUserTotalPointsAsync(userId, cancellationToken);
        var totalSpent = await _loyaltyRepository.GetUserTotalSpentAsync(userId, cancellationToken);
        var currentTier = await _loyaltyRepository.GetUserTierAsync(userId, cancellationToken);

        var isCurrentlyVip = currentTier != null &&
            string.Equals(currentTier.TierName, MembershipTiers.VIP, StringComparison.OrdinalIgnoreCase);

        if (isCurrentlyVip)
            return;

        var qualifiesForVip = totalPoints >= MembershipTiers.VipMinPoints ||
                              totalSpent >= MembershipTiers.VipMinSpent;

        if (!qualifiesForVip)
            return;

        var tiers = await _loyaltyRepository.GetAllTiersAsync(cancellationToken);
        var vipTier = tiers.FirstOrDefault(t =>
            string.Equals(t.TierName, MembershipTiers.VIP, StringComparison.OrdinalIgnoreCase));

        if (vipTier is null)
            return;

        await _loyaltyRepository.UpdateUserTierAsync(userId, vipTier.TierID, cancellationToken);
    }
}
