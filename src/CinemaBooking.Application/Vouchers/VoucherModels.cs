using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Vouchers;

public sealed record VoucherPage(IReadOnlyList<Voucher> Items, int PageIndex, int PageSize, int TotalItems);

public sealed record VoucherCommand(
    string VoucherCode, string? Category, string DiscountType, decimal DiscountValue,
    decimal? MinOrderValue, int? MaxUses, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil,
    string? Description, bool IsActive);

public sealed record VoucherResult(bool Succeeded, string? Error, Voucher? Voucher, string ErrorType = "validation");
