using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Vouchers;

public sealed class VoucherRequest
{
    [Required, MaxLength(50)] public string VoucherCode { get; set; } = string.Empty;
    [Required] public string DiscountType { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal DiscountValue { get; set; }
    [Range(0, double.MaxValue)] public decimal? MinOrderValue { get; set; }
    [Range(1, int.MaxValue)] public int? MaxUses { get; set; }
    [Required] public DateTimeOffset? ValidFrom { get; set; }
    [Required] public DateTimeOffset? ValidUntil { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Image is uploaded separately via POST /api/uploads/vouchers/image; the client
    // sends back the returned URL and public id here.
    [MaxLength(500)] public string? ImageUrl { get; set; }
    [MaxLength(200)] public string? ImagePublicId { get; set; }

    // Voucher type discriminator.
    // false (default) => Public voucher: usable via VoucherCode; RequiredPoints & ExchangeLimit must be null.
    // true            => Loyalty voucher: must be redeemed with points; RequiredPoints > 0 and ExchangeLimit > 0.
    public bool IsRedeemable { get; set; }
    [Range(1, int.MaxValue)] public int? RequiredPoints { get; set; }
    [Range(1, int.MaxValue)] public int? ExchangeLimit { get; set; }

    public List<VoucherRuleRequest>? Rules { get; set; }
}

public sealed record VoucherResponse(int VoucherId, string VoucherCode, string DiscountType,
    decimal DiscountValue, decimal? MinOrderValue, int? MaxUses, int UsedCount, DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil, string? ImageUrl, string? Description, bool IsActive, string Status, DateTime CreatedAt,
    List<VoucherRuleResponse>? Rules, bool IsRedeemable, int? RequiredPoints, int? ExchangeLimit);
public sealed record VoucherPageResponse(IReadOnlyList<VoucherResponse> Items, int PageIndex, int PageSize, int TotalItems, int TotalPages);

public sealed record RedeemableVoucherRuleResponse(
    string RuleType,
    string Operator,
    string RuleValue,
    string DisplayText);

// Response for GET /api/vouchers/redeemable. Same JSON schema as
// MyVoucherResponse so a single frontend voucher card can render both a
// customer's owned copies and the redeemable catalog. Ownership-only fields
// (Quantity, RedeemedAt, ExpiredAt, UsedAt) are always null here because a
// catalog entry has no per-user copy.
public sealed record RedeemableVoucherResponse(
    int VoucherId,
    string VoucherCode,
    string DiscountType,
    decimal DiscountValue,

    decimal? MinOrderValue,
    int? MaxUses,
    int UsedCount,

    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,

    string? ImageUrl,
    string? Description,

    bool IsActive,
    string Status,

    DateTime CreatedAt,

    List<RedeemableVoucherRuleResponse> VoucherRules,

    bool IsRedeemable,
    int RequiredPoints,
    int? ExchangeLimit,

    int? Quantity,
    DateTimeOffset? RedeemedAt,
    DateTimeOffset? ExpiredAt,
    DateTimeOffset? UsedAt);

public sealed record RedeemVoucherRequest(int VoucherId);

public sealed record RedeemVoucherResponse(
    bool Success,
    int RemainingPoints,
    string VoucherCode,
    string? Message = null);

// Response for the `vouchers` array embedded in GET /api/users/lookup.
// Carries the full voucher metadata the staff UI needs, keeping the ownership
// `Status` as the per-copy UserVoucher status (available/used/expired) — NOT
// the admin voucher lifecycle string used by MyVoucherResponse.
public sealed record UserVoucherResponse(
    // Voucher information
    int VoucherId,
    string VoucherCode,
    string DiscountType,
    decimal DiscountValue,

    decimal? MinOrderValue,
    int? MaxUses,
    int UsedCount,

    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,

    // Display information
    string? ImageUrl,
    string? Description,

    bool IsActive,
    // Ownership status of this specific copy (available/used/expired), not the
    // voucher's lifecycle status.
    string Status,

    DateTime CreatedAt,

    List<RedeemableVoucherRuleResponse> VoucherRules,

    bool IsRedeemable,
    int? RequiredPoints,
    int? ExchangeLimit,

    // Ownership information
    int Quantity,

    // Lifecycle
    DateTimeOffset RedeemedAt,
    DateTimeOffset ExpiredAt,
    DateTimeOffset? UsedAt);

// Response for GET /api/vouchers/my-vouchers. Same set of voucher-metadata
// fields as UserVoucherResponse, but Status is the admin voucher lifecycle
// string (ACTIVE/EXPIRED/EXHAUSTED/UPCOMING/DISABLED) instead of the per-copy
// ownership status.
public sealed record MyVoucherResponse(
    int VoucherId,
    string VoucherCode,
    string DiscountType,
    decimal DiscountValue,

    decimal? MinOrderValue,
    int? MaxUses,
    int UsedCount,

    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,

    string? ImageUrl,
    string? Description,

    bool IsActive,
    string Status,

    DateTime CreatedAt,

    List<RedeemableVoucherRuleResponse> VoucherRules,

    bool IsRedeemable,
    int? RequiredPoints,
    int? ExchangeLimit,

    int Quantity,
    DateTimeOffset RedeemedAt,
    DateTimeOffset ExpiredAt,
    DateTimeOffset? UsedAt);

