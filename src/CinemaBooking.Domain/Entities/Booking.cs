namespace CinemaBooking.Domain.Entities;

public class Booking
{
    public int BookingID { get; set; }
    public string BookingCode { get; set; } = null!;
    public int? UserID { get; set; }
    public int? ShowtimeID { get; set; }
    public int? CreatedByStaffID { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public int? PointsEarned { get; set; }
    public int? PointsRedeemed { get; set; }
    public string Status { get; set; } = "pending";
    public string? QRCode { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Showtime? Showtime { get; set; }
    public User? CreatedByStaff { get; set; }
    public ICollection<BookingSeat> BookingSeats { get; set; } = [];
    public ICollection<SeatHold> SeatHolds { get; set; } = [];
    public ICollection<BookingFnB> BookingFnBs { get; set; } = [];
    public Payment? Payment { get; set; }
    public Invoice? Invoice { get; set; }
    public ICollection<Refund> Refunds { get; set; } = [];
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = [];
    public BookingVoucher? BookingVoucher { get; set; }
    public ICollection<LoyaltyPoints> LoyaltyPoints { get; set; } = [];
}
