namespace CinemaBooking.Domain.Entities;

public class Payment
{
    public int PaymentID { get; set; }
    public int BookingID { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? TransactionCode { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RefundReason { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public int? RefundedBy { get; set; }

    public Booking Booking { get; set; } = null!;
    public User? RefundedByUser { get; set; }
    public ICollection<PaymentSession> PaymentSessions { get; set; } = [];
    public ICollection<Refund> Refunds { get; set; } = [];
}
