namespace CinemaBooking.Domain.Entities;

public class Invoice
{
    public int InvoiceID { get; set; }
    public int BookingID { get; set; }
    public string InvoiceCode { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime IssuedAt { get; set; }

    public Booking Booking { get; set; } = null!;
}
