using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Bookings;

public sealed class HoldSeatsRequest
{
    [Required]
    public int ShowtimeId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Please select at least one seat")]
    public List<int> SeatIds { get; set; } = new();
}

public sealed class CreateBookingRequest
{
    [Required]
    public int ShowtimeId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Please select at least one seat")]
    public List<int> SeatIds { get; set; } = new();

    public List<BookingFnBItem> FnbItems { get; set; } = new();

    public string? VoucherCode { get; set; }
}

public sealed class BookingFnBItem
{
    [Required]
    public int ItemId { get; set; }

    [Required]
    [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99")]
    public int Quantity { get; set; }
}
