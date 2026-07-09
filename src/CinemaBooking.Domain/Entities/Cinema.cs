namespace CinemaBooking.Domain.Entities;

public class Cinema
{
    public int CinemaID { get; set; }
    public string CinemaName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Room> Rooms { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}
