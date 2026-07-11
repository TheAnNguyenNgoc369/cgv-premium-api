namespace CinemaBooking.Domain.Entities;

public class VoucherRule
{
    public int RuleID { get; set; }
    public int VoucherID { get; set; }
    public string RuleType { get; set; } = null!;
    public string RuleValue { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Voucher Voucher { get; set; } = null!;
}
