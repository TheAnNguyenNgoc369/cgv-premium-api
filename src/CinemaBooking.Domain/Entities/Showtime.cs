namespace CinemaBooking.Domain.Entities;

public class Showtime
{
    public int ShowtimeID { get; set; }
    public int MovieID { get; set; }
    public int RoomID { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePrice { get; set; }
    public decimal RoomExtraPrice { get; set; }
    public string Status { get; set; } = "scheduled";
    public DateTime CreatedAt { get; set; }

    public Movie Movie { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public ICollection<SeatHold> SeatHolds { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
