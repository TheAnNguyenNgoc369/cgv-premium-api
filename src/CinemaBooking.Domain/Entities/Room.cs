using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBooking.Domain.Entities;

public class Room
{
    public int RoomID { get; set; }
    public int CinemaID { get; set; }
    public string RoomName { get; set; } = null!;
    public int RoomTypeID { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }

    public Cinema Cinema { get; set; } = null!;
    public RoomType RoomType { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = [];
    public ICollection<Showtime> Showtimes { get; set; } = [];
}
