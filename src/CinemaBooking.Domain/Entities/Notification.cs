namespace CinemaBooking.Domain.Entities;

public class Notification
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
