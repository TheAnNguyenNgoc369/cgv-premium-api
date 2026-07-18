using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Bookings;

public sealed class CalculatePricingRequest
{
    public int? CustomerId { get; set; }

    public int? ShowtimeId { get; set; }

    public List<int> SeatIds { get; set; } = new();

    public List<BookingFnBItem> FnbItems { get; set; } = new();

    public string? VoucherCode { get; set; }
}
