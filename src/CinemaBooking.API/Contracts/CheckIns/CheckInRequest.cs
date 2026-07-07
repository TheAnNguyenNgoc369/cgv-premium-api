using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class CheckInRequest
{
    [Required(ErrorMessage = "Booking ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Booking ID must be a positive number")]
    public int BookingId { get; set; }
}
