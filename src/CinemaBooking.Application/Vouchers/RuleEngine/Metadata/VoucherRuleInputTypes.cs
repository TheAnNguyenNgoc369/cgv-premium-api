namespace CinemaBooking.Application.Vouchers.RuleEngine.Metadata;

/// <summary>
/// Supported UI input types the frontend may render for a voucher rule.
/// Kept as constants (instead of an enum) so the exact wire values stay
/// stable regardless of C# naming changes.
/// </summary>
public static class VoucherRuleInputTypes
{
    public const string Select = "select";
    public const string MultiSelect = "multiselect";
    public const string Text = "text";
    public const string Number = "number";
}
