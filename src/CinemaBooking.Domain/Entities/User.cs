namespace CinemaBooking.Domain.Entities;

public class User
{
    public int UserID { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime? EmailVerifiedAt { get; set; }
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public string? AvatarURL { get; set; }
    public string Role { get; set; } = null!;
    public int? CinemaID { get; set; }
    public string Status { get; set; } = "unverified";
    public int? LoyaltyTierID { get; set; }
    public int TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Cinema? Cinema { get; set; }
    public LoyaltyTier? LoyaltyTier { get; set; }
    public Wallet? Wallet { get; set; }
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = [];
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
    public ICollection<SeatHold> SeatHolds { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Booking> StaffBookings { get; set; } = [];
    public ICollection<Ticket> CheckedInTickets { get; set; } = [];
    public ICollection<Refund> ProcessedRefunds { get; set; } = [];
    public ICollection<LoyaltyPoints> LoyaltyPoints { get; set; } = [];
    public ICollection<AdminActionLog> AdminActions { get; set; } = [];
    public ICollection<AdminActionLog> TargetedAdminActions { get; set; } = [];
    public ICollection<EmailLog> EmailLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
