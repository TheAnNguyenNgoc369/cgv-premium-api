using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Please enter email")]
    [EmailAddress(ErrorMessage = "Email is invalid")]
    [MaxLength(150, ErrorMessage = "Email do not exceeds 150 chracters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter password")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
