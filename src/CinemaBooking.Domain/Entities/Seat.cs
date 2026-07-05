namespace CinemaBooking.Domain.Entities;

public class Seat
{
    public int SeatID { get; set; }
    public int RoomID { get; set; }
    public string SeatRow { get; set; } = null!;
    public int SeatCol { get; set; }
    public int? SeatTypeID { get; set; }
    public string Status { get; set; } = "active";
    public bool IsGap { get; set; }
    public bool IsCurrentLayout { get; set; } = true;

    public Room Room { get; set; } = null!;
    public SeatType? SeatType { get; set; }
    public ICollection<SeatHold> SeatHolds { get; set; } = [];
    public ICollection<BookingSeat> BookingSeats { get; set; } = [];
}
