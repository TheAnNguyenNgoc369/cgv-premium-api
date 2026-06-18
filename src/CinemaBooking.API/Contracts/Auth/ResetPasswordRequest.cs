using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Auth;

public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "Please enter token")]
    [MaxLength(255, ErrorMessage = "Token is invalid")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter new password")]
    [MinLength(6, ErrorMessage = "Password atlease 6 characters")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please xác nhận password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password doesn't match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
