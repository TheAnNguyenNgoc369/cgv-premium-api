using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed class ChangeUserRoleRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
    public int? CinemaId { get; set; }
}
