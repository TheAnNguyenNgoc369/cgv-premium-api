using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Users;

public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Please enter current password")]
    [DataType(DataType.Password)]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter new password")]
    [MinLength(6, ErrorMessage = "Password must contain at least 6 characters")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$",
        ErrorMessage = "Password must contain at least 1 uppercase letter, 1 number, and 1 special character")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password does not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
