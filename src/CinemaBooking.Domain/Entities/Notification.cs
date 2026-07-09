namespace CinemaBooking.Domain.Entities;

public class Notification
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
