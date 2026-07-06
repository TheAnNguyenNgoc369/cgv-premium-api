using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CinemaBooking.API.Contracts.Vouchers;

public sealed class VoucherRequest
{
    [Required, MaxLength(50)] public string VoucherCode { get; set; } = string.Empty;
    public string? Category { get; set; }
    [Required] public string DiscountType { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal DiscountValue { get; set; }
    [Range(0, double.MaxValue)] public decimal? MinOrderValue { get; set; }
    [Range(1, int.MaxValue)] public int? MaxUses { get; set; }
    [Required] public DateTimeOffset? ValidFrom { get; set; }
    [Required] public DateTimeOffset? ValidUntil { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public IFormFile? Image { get; set; }
}

public sealed record VoucherResponse(int VoucherId, string VoucherCode, string? Category, string DiscountType,
    decimal DiscountValue, decimal? MinOrderValue, int? MaxUses, int UsedCount, DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil, string? ImageUrl, string? Description, bool IsActive, string Status, DateTime CreatedAt);
public sealed record VoucherPageResponse(IReadOnlyList<VoucherResponse> Items, int PageIndex, int PageSize, int TotalItems, int TotalPages);
