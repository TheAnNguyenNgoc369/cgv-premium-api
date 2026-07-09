using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Please enter full name")]
    [MaxLength(100, ErrorMessage = "Full name must not exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter email")]
    [EmailAddress(ErrorMessage = "Email is invalid")]
    [MaxLength(150, ErrorMessage = "Email must not exceed 150 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter phone number")]
    [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Phone number must contain 10 digits and start with 0")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter password")]
    [MinLength(6, ErrorMessage = "Password must contain at least 6 characters")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$",
        ErrorMessage = "Password must contain at least 1 uppercase letter, 1 number, and 1 special character")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password")]
    [Compare(nameof(Password), ErrorMessage = "Confirm password does not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
