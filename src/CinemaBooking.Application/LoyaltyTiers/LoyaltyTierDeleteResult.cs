namespace CinemaBooking.Application.LoyaltyTiers;

public sealed record LoyaltyTierDeleteResult(
    bool Succeeded,
    string? ErrorMessage,
    bool IsConflict = false,
    bool IsNotFound = false);
