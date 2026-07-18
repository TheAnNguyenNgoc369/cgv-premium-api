using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Vouchers;

public sealed record VoucherPage(IReadOnlyList<Voucher> Items, int PageIndex, int PageSize, int TotalItems);

public sealed record VoucherCommand(
    string VoucherCode, string DiscountType, decimal DiscountValue,
    decimal? MinOrderValue, int? MaxUses, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil,
    string? Description, bool IsActive, string? ImageUrl, string? ImagePublicId, List<VoucherRuleDto>? Rules,
    bool IsRedeemable = false, int? RequiredPoints = null, int? ExchangeLimit = null);

public sealed record VoucherRuleDto(
    string RuleType,
    string RuleValue);

public sealed record VoucherResult(bool Succeeded, string? Error, Voucher? Voucher, string ErrorType = "validation");

public sealed record RedeemableVouchersResult(bool Succeeded, List<Voucher> Vouchers, string? Error = null);

public sealed record RedeemVoucherResult(bool Succeeded, int RemainingPoints, string VoucherCode, string? Error = null, string ErrorType = "validation");

public sealed record UserVouchersResult(bool Succeeded, List<UserVoucher> Vouchers, string? Error = null);

public sealed record UserRedeemableVoucher(
    int VoucherId,
    string VoucherCode,
    string DiscountType,
    decimal DiscountValue,
    int RequiredPoints,
    int? ExchangeLimit,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    string? ImageUrl,
    string? Description);

public sealed record UserRedeemableVouchersResult(bool Succeeded, List<UserRedeemableVoucher> Vouchers, string? Error = null);
