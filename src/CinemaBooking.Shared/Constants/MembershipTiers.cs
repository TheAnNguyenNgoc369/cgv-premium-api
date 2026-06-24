namespace CinemaBooking.Shared.Constants;

public static class MembershipTiers
{
    public const string Member = "Member";
    public const string VIP = "VIP";

    public const int VipMinPoints = 200;
    public const decimal VipMinSpent = 2_000_000m;

    public const decimal MemberDiscountRate = 0m;
    public const decimal VipDiscountRate = 0.10m;

    public const decimal PointsPerVnd = 0.0001m; // 1 point per 10,000 VND
}

public static class LoyaltyTransactionTypes
{
    public const string Earned = "earn";
    public const string Redeemed = "redeem";
    public const string Expired = "expire";
    public const string Adjusted = "adjust";
}
