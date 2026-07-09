using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed class ChangeUserStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
