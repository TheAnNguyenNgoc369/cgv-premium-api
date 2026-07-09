namespace CinemaBooking.Domain.Entities;

public class AdminActionLog
{
    public int LogID { get; set; }
    public int AdminID { get; set; }
    public int? TargetUserID { get; set; }
    public string? TargetTable { get; set; }
    public int? TargetID { get; set; }
    public string ActionType { get; set; } = null!;
    public string? Description { get; set; }
    public string? IPAddress { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Admin { get; set; } = null!;
    public User? TargetUser { get; set; }
}
