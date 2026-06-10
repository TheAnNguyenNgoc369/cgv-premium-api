namespace CinemaBooking.Domain.Entities;

public class SeatHold
{
    public int HoldID { get; set; }
    public int SeatID { get; set; }
    public int ShowtimeID { get; set; }
    public int UserID { get; set; }
    public DateTime HeldAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "holding";

    public Seat Seat { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
    public User User { get; set; } = null!;
}
