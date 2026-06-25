using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Auth;

public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Please enter email")]
    [EmailAddress(ErrorMessage = "Email is invalid")]
    [MaxLength(150, ErrorMessage = "Email must not exceed 150 characters")]
    public string Email { get; set; } = string.Empty;
}
