namespace CinemaBooking.Application.CheckIns;

public sealed class CheckInLookupResult
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public MovieInfo Movie { get; set; } = null!;
    public CinemaInfo Cinema { get; set; } = null!;
    public RoomInfo Room { get; set; } = null!;
    public ShowtimeInfo Showtime { get; set; } = null!;
    public string PaymentStatus { get; set; } = string.Empty;
    public string BookingStatus { get; set; } = string.Empty;
    public bool CheckedIn { get; set; }
    public List<SeatInfo> Seats { get; set; } = [];
    public List<ProductInfo> Products { get; set; } = [];
    public bool IsFromOtherCinema { get; set; }
    public string? WarningMessage { get; set; }

    public sealed class MovieInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Rating { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string? PosterURL { get; set; }
    }

    public sealed class CinemaInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public sealed class RoomInfo
    {
        public string Name { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
    }

    public sealed class ShowtimeInfo
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public sealed class SeatInfo
    {
        public string Row { get; set; } = string.Empty;
        public int Column { get; set; }
        public string SeatType { get; set; } = string.Empty;
        public decimal TicketPrice { get; set; }
        public bool IsCheckedIn { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }

    public sealed class ProductInfo
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
