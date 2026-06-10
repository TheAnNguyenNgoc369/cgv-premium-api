namespace CinemaBooking.Domain.Entities;

public class BookingVoucher
{
    public int BookingVoucherID { get; set; }
    public int BookingID { get; set; }
    public int VoucherID { get; set; }
    public decimal DiscountApplied { get; set; }
    public DateTime UsedAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public Voucher Voucher { get; set; } = null!;
}
