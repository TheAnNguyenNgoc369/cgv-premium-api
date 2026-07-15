using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.LoyaltyTiers;

public sealed record LoyaltyTierResult(
    bool Succeeded,
    string? ErrorMessage,
    LoyaltyTier? Tier,
    bool IsConflict = false,
    bool IsNotFound = false);
