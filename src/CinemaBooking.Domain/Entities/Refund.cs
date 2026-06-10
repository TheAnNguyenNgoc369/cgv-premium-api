namespace CinemaBooking.Domain.Entities;

public class Refund
{
    public int RefundID { get; set; }
    public int BookingID { get; set; }
    public int? PaymentID { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "pending";
    public int? ProcessedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public Payment? Payment { get; set; }
    public User? ProcessedByUser { get; set; }
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = [];
}
