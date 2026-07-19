using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Bookings;

public sealed class LookupBookingFnbRequest
{
    [Required(ErrorMessage = "Booking code is required")]
    [MaxLength(50, ErrorMessage = "Booking code must not exceed 50 characters")]
    public string BookingCode { get; set; } = null!;
}