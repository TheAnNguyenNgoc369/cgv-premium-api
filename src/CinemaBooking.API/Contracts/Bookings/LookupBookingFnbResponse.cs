namespace CinemaBooking.API.Contracts.Bookings;

public sealed class LookupBookingFnbResponse
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAvatarURL { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<FnbItemInfo> FnbItems { get; set; } = [];

    public sealed class FnbItemInfo
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ImageURL { get; set; }
        public int Quantity { get; set; }
        public bool PickedUp { get; set; }
    }
}