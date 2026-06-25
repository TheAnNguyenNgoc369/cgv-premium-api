using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Please enter email")]
    [EmailAddress(ErrorMessage = "Email is invalid")]
    [MaxLength(150, ErrorMessage = "Email do not exceeds 150 chracters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter password")]
    [MinLength(6, ErrorMessage = "Password atleast 6 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
