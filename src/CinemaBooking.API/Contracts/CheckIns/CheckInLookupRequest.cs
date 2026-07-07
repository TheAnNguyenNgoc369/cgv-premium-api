using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class CheckInLookupRequest
{
    [Required(ErrorMessage = "QR Code is required")]
    [MaxLength(100, ErrorMessage = "QR Code must not exceed 100 characters")]
    public string QRCode { get; set; } = string.Empty;
}
