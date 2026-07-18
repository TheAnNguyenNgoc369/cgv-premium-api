namespace CinemaBooking.Domain.Entities;

public class MovieReview
{
    public int ReviewId { get; set; }
    public int MovieId { get; set; }
    public int UserId { get; set; }
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? HiddenAt { get; set; }
    public int? HiddenBy { get; set; }

    public Movie? Movie { get; set; }
    public User? User { get; set; }
    public Booking? Booking { get; set; }
    public User? HiddenByUser { get; set; }
}
