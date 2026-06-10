namespace CinemaBooking.Domain.Entities;

public class BookingFnB
{
    public int BookingFnBID { get; set; }
    public int BookingID { get; set; }
    public int ItemID { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    public Booking Booking { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
