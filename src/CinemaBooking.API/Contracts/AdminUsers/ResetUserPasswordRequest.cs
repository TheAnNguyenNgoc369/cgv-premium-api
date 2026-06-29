using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed class ResetUserPasswordRequest
{
    [Required, MinLength(8)]
    [RegularExpression(@"^(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
