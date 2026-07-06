namespace CinemaBooking.Domain.Entities;

public class ShowtimeType
{
    public int ShowtimeTypeID { get; set; }
    public int CinemaID { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Cinema Cinema { get; set; } = null!;
    public ICollection<ShowtimeTypeSlot> Slots { get; set; } = [];
    public ICollection<Showtime> Showtimes { get; set; } = [];
}
