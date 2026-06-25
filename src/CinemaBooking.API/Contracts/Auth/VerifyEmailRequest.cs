using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Auth;

public sealed class VerifyEmailRequest
{
    [Required(ErrorMessage = "Please enter code")]
    [MaxLength(255, ErrorMessage = "Code is invalid")]
    public string Code { get; set; } = string.Empty;
}
