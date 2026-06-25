namespace CinemaBooking.API.Contracts.Membership;

public sealed record MembershipResponse(
    string Tier,
    int TotalPoints,
    decimal TotalSpent,
    decimal DiscountPercent,
    bool IsVip,
    int PointsToVip
);

public sealed record TierResponse(
    int TierID,
    string TierName,
    int MinPoints,
    decimal DiscountRate
);

public sealed record PointHistoryResponse(
    int PointsDelta,
    string TransactionType,
    string? Description,
    DateTime CreatedAt
);
