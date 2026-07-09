namespace CinemaBooking.Domain.Entities;

public class ShowtimeTypeSlot
{
    public int SlotID { get; set; }
    public int ShowtimeTypeID { get; set; }
    public TimeSpan StartTime { get; set; }
    public ShowtimeType ShowtimeType { get; set; } = null!;
}
