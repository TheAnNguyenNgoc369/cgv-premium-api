using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.LoyaltyTiers;

public sealed class LoyaltyTierRequest
{
    [Required]
    public string? TierName { get; init; }

    [Range(0, int.MaxValue)]
    public int MinPoints { get; init; }

    [Range(typeof(decimal), "0", "1")]
    public decimal DiscountRate { get; init; }

    [Range(0, int.MaxValue)]
    public int MaxRefundPerMonth { get; init; }
}
