namespace CinemaBooking.API.Contracts.LoyaltyTiers;

public sealed record LoyaltyTierResponse(
    int TierID,
    string TierName,
    int MinPoints,
    decimal DiscountRate,
    int MaxRefundPerMonth);
