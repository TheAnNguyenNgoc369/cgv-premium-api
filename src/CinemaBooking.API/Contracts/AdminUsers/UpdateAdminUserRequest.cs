using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed class UpdateAdminUserRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression(@"^0[0-9]{9}$")]
    public string Phone { get; set; } = string.Empty;

    public int? CinemaId { get; set; }
}
