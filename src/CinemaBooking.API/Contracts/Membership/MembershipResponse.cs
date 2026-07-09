using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Membership;

public sealed record MembershipResponse(
    string CurrentTier,
    string? NextTier,
    int PointsToNextTier,
    int TotalPoints,
    decimal TotalSpent,
    decimal DiscountPercent,
    [property: JsonPropertyName("total_refunds")] int TotalRefunds,
    [property: JsonPropertyName("used_refunds")] int UsedRefunds
);

public sealed record TierResponse(
    int TierID,
    string TierName,
    int MinPoints,
    decimal DiscountRate,
    [property: JsonPropertyName("total_refunds")] int TotalRefunds
);

public sealed record PointHistoryResponse(
    int PointsDelta,
    string TransactionType,
    string? Description,
    DateTime CreatedAt
);
