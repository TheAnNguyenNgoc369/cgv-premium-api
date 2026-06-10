namespace CinemaBooking.Domain.Entities;

public class LoyaltyPoints
{
    public int PointID { get; set; }
    public int UserID { get; set; }
    public int? BookingID { get; set; }
    public int PointsDelta { get; set; }
    public string TransactionType { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Booking? Booking { get; set; }
}
