namespace CinemaBooking.Domain.Entities;

public class Wallet
{
    public int WalletID { get; set; }
    public int UserID { get; set; }
    public decimal Balance { get; set; }

    public User User { get; set; } = null!;
    public ICollection<WalletTransaction> Transactions { get; set; } = [];
}
