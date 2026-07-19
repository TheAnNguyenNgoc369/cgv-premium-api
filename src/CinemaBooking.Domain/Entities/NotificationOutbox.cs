namespace CinemaBooking.Domain.Entities;

public sealed class NotificationOutbox
{
    public long NotificationOutboxID { get; set; }
    public string EventId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public int ReferenceId { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? OccurredAt { get; set; }
    public string Status { get; set; } = "pending";
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Message { get; set; }
}
