namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class CheckedInSeatDto
{
    public string SeatCode { get; set; } = string.Empty;
    public string SeatType { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
    public DateTime CheckedInAt { get; set; }
}
