namespace CinemaBooking.Domain.Entities;

public class Seat
{
    public int SeatID { get; set; }
    public int RoomID { get; set; }
    public string SeatRow { get; set; } = null!;
    public int SeatCol { get; set; }
    public int SeatTypeID { get; set; }
    public string Status { get; set; } = "active";

    public Room Room { get; set; } = null!;
    public SeatType SeatType { get; set; } = null!;
    public ICollection<SeatHold> SeatHolds { get; set; } = [];
    public ICollection<BookingSeat> BookingSeats { get; set; } = [];
}
