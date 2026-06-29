using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed class CreateAdminUserRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression(@"^0[0-9]{9}$")]
    public string Phone { get; set; } = string.Empty;

    [Required, MinLength(8)]
    [RegularExpression(@"^(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public string? Status { get; set; }
    public int? CinemaId { get; set; }
}
