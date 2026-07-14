namespace CinemaBooking.Application.CheckIns;

public sealed class CheckInHistoryResult
{
    public List<CheckInRecord> Records { get; set; } = [];
    public int TotalCount { get; set; }

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
        public List<CheckedInSeatRecord> CheckedInSeats { get; set; } = [];
    }

    public sealed class CheckedInSeatRecord
    {
        public string SeatCode { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty;
        public decimal TicketPrice { get; set; }
        public DateTime CheckedInAt { get; set; }
    }
}
