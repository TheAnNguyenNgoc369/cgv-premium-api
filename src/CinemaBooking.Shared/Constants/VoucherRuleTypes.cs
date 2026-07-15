namespace CinemaBooking.Shared.Constants;

/// <summary>
/// Constants for voucher rule types supported by the rule engine
/// </summary>
public static class VoucherRuleTypes
{
    public const string ApplyScope = "ApplyScope";
    public const string Cinema = "Cinema";
    public const string Movie = "Movie";
    public const string Room = "Room";
    public const string SeatType = "SeatType";
    public const string Membership = "Membership";
    public const string PaymentMethod = "PaymentMethod";
    public const string DayOfWeek = "DayOfWeek";
    public const string Product = "Product";
    public const string FoodCategory = "FoodCategory";
}

/// <summary>
/// Constants for voucher apply scope values
/// </summary>
public static class ApplyScopes
{
    public const string Order = "Order";
    public const string Ticket = "Ticket";
    public const string Food = "Food";
}
