namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class CheckInHistoryRequest
{
    public int? StaffId { get; set; }
    public int? CinemaId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
