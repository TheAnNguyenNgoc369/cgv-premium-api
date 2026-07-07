namespace CinemaBooking.Domain.Entities;

public class UserVoucher
{
    public int UserVoucherID { get; set; }
    public int UserID { get; set; }
    public int VoucherID { get; set; }
    public DateTime RedeemedAt { get; set; }
    public DateTime ExpiredAt { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? UsedAt { get; set; }
    public int? BookingID { get; set; }

    public User User { get; set; } = null!;
    public Voucher Voucher { get; set; } = null!;
    public Booking? Booking { get; set; }
}
