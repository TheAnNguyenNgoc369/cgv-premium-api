using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Users;

public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Please enter fullname")]
    [MaxLength(100, ErrorMessage = "Fullname do not exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Phone number is invalid (Start with 0, contains 10 number)")]
    public string? Phone { get; set; }

    [MaxLength(500, ErrorMessage = "Avatar URL do not exceeds 500 characters")]
    [Url(ErrorMessage = "Avatar URL is invalid")]
    public string? AvatarURL { get; set; }
}
