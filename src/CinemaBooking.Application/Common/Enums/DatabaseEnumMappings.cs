namespace CinemaBooking.Application.Common.Enums;

public static class DatabaseEnumMappings
{
    public static readonly IReadOnlyDictionary<string, string> CinemaStatuses =
        EnumValueMapper.CreateMappings("active", "inactive", "maintenance");
    public static readonly IReadOnlyDictionary<string, string> MovieStatuses =
        EnumValueMapper.CreateMappings("coming_soon", "now_showing", "ended");
    public static readonly IReadOnlyDictionary<string, string> MovieAgeRatings =
        EnumValueMapper.CreateMappings("P", "C13", "C16", "C18");
    public static readonly IReadOnlyDictionary<string, string> ShowtimeStatuses =
        EnumValueMapper.CreateMappings("scheduled", "ongoing", "completed", "cancelled");
    public static readonly IReadOnlyDictionary<string, string> RoomStatuses =
        EnumValueMapper.CreateMappings("active", "maintenance", "inactive");
    public static readonly IReadOnlyDictionary<string, string> RoomTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["STANDARD"] = "Standard", ["VIP"] = "VIP", ["IMAX"] = "IMAX",
            ["THREE_D"] = "3D", ["3D"] = "3D"
        };
    public static readonly IReadOnlyDictionary<string, string> SeatStatuses =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ACTIVE"] = "active", ["INACTIVE"] = "inactive",
            ["DISABLED"] = "inactive", ["MAINTENANCE"] = "inactive"
        };
    public static readonly IReadOnlyDictionary<string, string> PaymentMethods =
        EnumValueMapper.CreateMappings("vnpay", "momo", "credit_card", "banking", "cash", "wallet");
    public static readonly IReadOnlyDictionary<string, string> BookingStatuses =
        EnumValueMapper.CreateMappings("pending", "paid", "cancelled", "refunded", "used",
            "expired", "payment_failed", "partially_refunded");
    public static readonly IReadOnlyDictionary<string, string> PaymentStatuses =
        EnumValueMapper.CreateMappings("pending", "success", "failed", "refunded", "cancelled", "expired");
    public static readonly IReadOnlyDictionary<string, string> PaymentSessionStatuses =
        EnumValueMapper.CreateMappings("waiting", "processing", "completed", "expired", "cancelled");
    public static readonly IReadOnlyDictionary<string, string> PaymentGateways =
        EnumValueMapper.CreateMappings("vnpay", "momo");
    public static readonly IReadOnlyDictionary<string, string> WalletTransactionTypes =
        EnumValueMapper.CreateMappings("top_up", "payment", "refund");
    public static readonly IReadOnlyDictionary<string, string> LoyaltyTransactionTypes =
        EnumValueMapper.CreateMappings("earn", "redeem", "expire", "adjust");
    public static readonly IReadOnlyDictionary<string, string> LoyaltyTierNames =
        EnumValueMapper.CreateMappings("silver", "gold", "platinum", "megavip");
    public static readonly IReadOnlyDictionary<string, string> UserRoles =
        EnumValueMapper.CreateMappings("customer", "staff", "admin", "manager");
    public static readonly IReadOnlyDictionary<string, string> UserStatuses =
        EnumValueMapper.CreateMappings("unverified", "active", "locked", "inactive");
    public static readonly IReadOnlyDictionary<string, string> SeatHoldStatuses =
        EnumValueMapper.CreateMappings("holding", "confirmed", "released", "expired");
    public static readonly IReadOnlyDictionary<string, string> TicketStatuses =
        EnumValueMapper.CreateMappings("valid", "used", "cancelled");
    public static readonly IReadOnlyDictionary<string, string> RefundStatuses =
        EnumValueMapper.CreateMappings("pending", "approved", "rejected", "processing", "completed", "failed");
    public static readonly IReadOnlyDictionary<string, string> ProductTypes =
        EnumValueMapper.CreateMappings("combo", "snack", "beverage", "dessert");
    public static readonly IReadOnlyDictionary<string, string> ProductStatuses =
        EnumValueMapper.CreateMappings("in_stock", "low_stock", "out_of_stock", "inactive");
    public static readonly IReadOnlyDictionary<string, string> VoucherDiscountTypes =
        EnumValueMapper.CreateMappings("percent", "fixed");
    public static readonly IReadOnlyDictionary<string, string> VoucherCategories =
        EnumValueMapper.CreateMappings("Discount", "Combo", "Cashback");
    public static readonly IReadOnlyDictionary<string, string> NotificationTypes =
        EnumValueMapper.CreateMappings("booking", "payment", "refund", "promotion", "system");
    public static readonly IReadOnlyDictionary<string, string> EmailEventTypes =
        EnumValueMapper.CreateMappings("register", "booking_confirmed", "booking_cancelled",
            "forgot_password", "refund_processed", "points_earned", "reward_redeemed");
    public static readonly IReadOnlyDictionary<string, string> EmailDeliveryStatuses =
        EnumValueMapper.CreateMappings("sent", "failed", "retrying");
    public static readonly IReadOnlyDictionary<string, string> AdminActionTypes =
        EnumValueMapper.CreateMappings("lock_user", "unlock_user", "change_role", "cancel_booking",
            "refund_processed", "payment_viewed", "booking_created", "account_status_changed",
            "create_user", "update_user", "delete_user", "deactivate_user");
}
