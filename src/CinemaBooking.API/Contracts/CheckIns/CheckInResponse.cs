namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class CheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
}
