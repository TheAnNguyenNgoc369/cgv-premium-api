namespace CinemaBooking.Domain.Entities;

public class EmailLog
{
    public int EmailLogID { get; set; }
    public int? UserID { get; set; }
    public string ToEmail { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string DeliveryStatus { get; set; } = "sent";
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
