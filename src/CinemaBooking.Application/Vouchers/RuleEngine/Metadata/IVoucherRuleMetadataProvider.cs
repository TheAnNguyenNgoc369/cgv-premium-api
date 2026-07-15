namespace CinemaBooking.Application.Vouchers.RuleEngine.Metadata;

/// <summary>
/// Exposes UI-rendering metadata for every voucher rule type the backend
/// supports. Consumed by <c>GET /api/vouchers/rule-types</c>.
/// </summary>
public interface IVoucherRuleMetadataProvider
{
    /// <summary>
    /// Returns metadata for every supported voucher rule type, in a stable
    /// order suitable for direct rendering in an admin form.
    /// </summary>
    IReadOnlyList<VoucherRuleTypeMetadata> GetAll();
}
