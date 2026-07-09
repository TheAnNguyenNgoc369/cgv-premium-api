namespace CinemaBooking.Domain.Entities;

public class EmailLog
{
    public int EmailLogID { get; set; }
    public int? UserID { get; set; }
    public string ToEmail { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string HtmlBody { get; set; } = null!;
    public string? InlineImagesJson { get; set; }
    public string EventType { get; set; } = null!;
    public string DeliveryStatus { get; set; } = "pending";
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
