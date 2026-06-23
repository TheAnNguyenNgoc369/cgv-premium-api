using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Bookings;

public sealed class HoldSeatsRequest
{
    [Required]
    public int ShowtimeId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 ghế")]
    public List<int> SeatIds { get; set; } = new();
}

public sealed class CreateBookingRequest
{
    [Required]
    public int ShowtimeId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 ghế")]
    public List<int> SeatIds { get; set; } = new();

    public List<BookingFnBItem> FnbItems { get; set; } = new();

    public string? VoucherCode { get; set; }
}

public sealed class BookingFnBItem
{
    [Required]
    public int ItemId { get; set; }

    [Required]
    [Range(1, 99, ErrorMessage = "Số lượng phải từ 1-99")]
    public int Quantity { get; set; }
}
