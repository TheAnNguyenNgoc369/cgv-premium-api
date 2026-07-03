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
        var allTiers = await _loyaltyRepository.GetAllTiersAsync(cancellationToken);

        if (!allTiers.Any())
        {
            throw new InvalidOperationException("No loyalty tiers found in database. Please run migrations.");
        }

        var currentTier = allTiers
            .Where(t => totalPoints >= t.MinPoints)
            .OrderByDescending(t => t.MinPoints)
            .FirstOrDefault();

        // Fallback to lowest tier if user doesn't qualify for any tier yet
        currentTier ??= allTiers.OrderBy(t => t.MinPoints).FirstOrDefault();

        var currentTierName = currentTier?.TierName ?? "unknown";
        var discountRate = currentTier?.DiscountRate ?? 0m;
        var discountPercent = discountRate * 100;

        var nextTier = allTiers
            .Where(t => t.MinPoints > totalPoints)
            .OrderBy(t => t.MinPoints)
            .FirstOrDefault();

        var nextTierName = nextTier?.TierName;
        var pointsToNextTier = nextTier != null ? nextTier.MinPoints - totalPoints : 0;

        return new MembershipInfo(
            CurrentTier: currentTierName,
            NextTier: nextTierName,
            PointsToNextTier: pointsToNextTier,
            TotalPoints: totalPoints,
            TotalSpent: totalSpent,
            DiscountPercent: discountPercent
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
        if (await _loyaltyRepository.HasPointsForBookingAsync(bookingId, cancellationToken))
            return;

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
        var allTiers = await _loyaltyRepository.GetAllTiersAsync(cancellationToken);

        if (!allTiers.Any())
        {
            throw new InvalidOperationException("No loyalty tiers found in database. Please run migrations.");
        }

        var appropriateTier = allTiers
            .Where(t => totalPoints >= t.MinPoints)
            .OrderByDescending(t => t.MinPoints)
            .FirstOrDefault();

        if (appropriateTier is null)
            return;

        var currentTier = await _loyaltyRepository.GetUserTierAsync(userId, cancellationToken);

        if (currentTier?.TierID != appropriateTier.TierID)
        {
            await _loyaltyRepository.UpdateUserTierAsync(userId, appropriateTier.TierID, cancellationToken);
        }
    }
}
