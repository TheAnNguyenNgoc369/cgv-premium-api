namespace CinemaBooking.Domain.Entities;

public class WalletTransaction
{
    public int TransactionID { get; set; }
    public int WalletID { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string TransactionType { get; set; } = null!;
    public int? BookingID { get; set; }
    public int? RefundID { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public Wallet Wallet { get; set; } = null!;
    public Booking? Booking { get; set; }
    public Refund? Refund { get; set; }
}
