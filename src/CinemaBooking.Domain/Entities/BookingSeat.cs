namespace CinemaBooking.Domain.Entities;

public class BookingSeat
{
    public int BookingSeatID { get; set; }
    public int BookingID { get; set; }
    public int SeatID { get; set; }
    public decimal TicketPrice { get; set; }

    public Booking Booking { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}
