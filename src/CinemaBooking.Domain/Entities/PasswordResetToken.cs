namespace CinemaBooking.Domain.Entities;

public class PasswordResetToken
{
    public int TokenID { get; set; }
    public int UserID { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
