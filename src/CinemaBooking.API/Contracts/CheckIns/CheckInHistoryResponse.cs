namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class CheckInHistoryResponse
{
    public List<CheckInRecord> Records { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public sealed class CheckInRecord
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string CinemaName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public DateTime ShowtimeStart { get; set; }
        public DateTime CheckedInAt { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public int SeatCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
