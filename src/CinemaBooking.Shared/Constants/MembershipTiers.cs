namespace CinemaBooking.Shared.Constants;

public static class MembershipTiers
{
    // Tier names, MinPoints, and DiscountRate are retrieved dynamically from LoyaltyTiers table
    public const decimal PointsPerVnd = 0.001m; // 1 point per 1,000 VND
}

public static class LoyaltyTransactionTypes
{
    public const string Earned = "earn";
    public const string Redeemed = "redeem";
    public const string Expired = "expire";
    public const string Adjusted = "adjust";
}
