namespace CinemaBooking.Domain.Entities;

public class LoyaltyTier
{
    public int TierID { get; set; }
    public string TierName { get; set; } = null!;
    public int MinPoints { get; set; }
    public decimal DiscountRate { get; set; }
    public int MaxRefundPerMonth { get; set; }

    public ICollection<User> Users { get; set; } = [];
}
