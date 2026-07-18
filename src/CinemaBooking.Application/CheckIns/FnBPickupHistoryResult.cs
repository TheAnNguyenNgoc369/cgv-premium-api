namespace CinemaBooking.Application.CheckIns;

public sealed class FnBPickupHistoryResult
{
    public List<FnBPickupRecord> Records { get; set; } = [];
    public int TotalCount { get; set; }

    public sealed class FnBPickupRecord
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string CinemaName { get; set; } = string.Empty;
        public DateTime PickedUpAt { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<FnBPickupItemRecord> Items { get; set; } = [];
    }

    public sealed class FnBPickupItemRecord
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }
}