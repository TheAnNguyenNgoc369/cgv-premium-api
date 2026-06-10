namespace CinemaBooking.Domain.Entities;

public class Ticket
{
    public int TicketID { get; set; }
    public int BookingSeatID { get; set; }
    public string QRCode { get; set; } = null!;
    public string Status { get; set; } = "valid";
    public DateTime? CheckedInAt { get; set; }
    public int? CheckedInByID { get; set; }

    public BookingSeat BookingSeat { get; set; } = null!;
    public User? CheckedInBy { get; set; }
}
