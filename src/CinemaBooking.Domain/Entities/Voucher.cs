namespace CinemaBooking.Domain.Entities;

public class Voucher
{
    public int VoucherID { get; set; }
    public string VoucherCode { get; set; } = null!;
    public string? Category { get; set; }
    public string DiscountType { get; set; } = null!;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public string? ImageURL { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<BookingVoucher> BookingVouchers { get; set; } = [];
}
