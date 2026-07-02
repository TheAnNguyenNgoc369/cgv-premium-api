using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Bookings;

public sealed class ReleaseSeatHoldsRequest
{
    [Required]
    public int ShowtimeId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Please select at least one seat")]
    public List<int> SeatIds { get; set; } = [];
}
