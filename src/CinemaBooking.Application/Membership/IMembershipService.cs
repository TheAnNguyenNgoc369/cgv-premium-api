using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Membership;

public interface IMembershipService
{
    Task<MembershipInfo> GetMyMembershipAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<LoyaltyTier>> GetTiersAsync(
        CancellationToken cancellationToken = default);

    Task<CreateTierResult> CreateTierAsync(
        string tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken = default);

    Task<List<LoyaltyPointHistory>> GetPointHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddPointsAfterPaymentSuccessAsync(
        int userId,
        int bookingId,
        decimal finalAmount,
        CancellationToken cancellationToken = default);

    Task CheckAndUpgradeTierAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

public sealed record MembershipInfo(
    string CurrentTier,
    string? NextTier,
    int PointsToNextTier,
    int TotalPoints,
    decimal TotalSpent,
    decimal DiscountPercent,
    int TotalRefunds,
    int UsedRefunds
);

public sealed record LoyaltyPointHistory(
    int PointsDelta,
    string TransactionType,
    string? Description,
    DateTime CreatedAt
);

public sealed record CreateTierResult(
    bool Succeeded,
    string? ErrorMessage,
    LoyaltyTier? Tier
);
