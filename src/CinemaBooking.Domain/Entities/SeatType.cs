namespace CinemaBooking.Domain.Entities;

public class SeatType
{
    public int SeatTypeID { get; set; }
    public string TypeName { get; set; } = null!;
    public decimal ExtraPrice { get; set; }

    public ICollection<Seat> Seats { get; set; } = [];
}
