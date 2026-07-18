using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.Domain.Entities;

public class EmailVerificationToken
{
    public int TokenID { get; set; }
    public int UserID { get; set; }
    
    [StringLength(6, MinimumLength = 6)]
    public string Token { get; set; } = null!;
    
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
