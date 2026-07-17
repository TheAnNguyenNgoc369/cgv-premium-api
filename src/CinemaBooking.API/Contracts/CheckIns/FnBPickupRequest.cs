using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class FnBPickupRequest
{
    [Required]
    public string BookingCode { get; set; } = null!;
}