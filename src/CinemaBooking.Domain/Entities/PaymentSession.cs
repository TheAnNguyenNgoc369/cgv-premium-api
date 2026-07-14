namespace CinemaBooking.Domain.Entities;

public class PaymentSession
{
    public int SessionID { get; set; }
    public int PaymentID { get; set; }
    public string GatewayName { get; set; } = null!;
    public string? GatewayOrderNo { get; set; }
    public string? QRCodeURL { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "waiting";
    public DateTime CreatedAt { get; set; }

    public Payment Payment { get; set; } = null!;
}
