namespace CinemaBooking.API.Contracts.Membership;

public sealed record MembershipResponse(
    string CurrentTier,
    string? NextTier,
    int PointsToNextTier,
    int TotalPoints,
    decimal TotalSpent,
    decimal DiscountPercent
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
