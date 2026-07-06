namespace CinemaBooking.Application.Refunds;

public sealed class RefundResult
{
    public int RefundId { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal WalletBalance { get; set; }
    public string Status { get; set; } = null!;
}
