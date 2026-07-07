namespace CinemaBooking.Domain.Entities;

public class RoomType
{
    public int RoomTypeID { get; set; }
    public string TypeName { get; set; } = null!;
    public decimal ExtraPrice { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Room> Rooms { get; set; } = [];
}
