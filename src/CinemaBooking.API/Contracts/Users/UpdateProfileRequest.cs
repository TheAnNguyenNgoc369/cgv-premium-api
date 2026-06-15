using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Users;

public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Vui long nhap ho ten")]
    [MaxLength(100, ErrorMessage = "Ho ten khong duoc vuot qua 100 ky tu")]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "So dien thoai khong hop le (phai co 10 so, bat dau bang 0)")]
    public string? Phone { get; set; }

    [MaxLength(500, ErrorMessage = "Avatar URL khong duoc vuot qua 500 ky tu")]
    [Url(ErrorMessage = "Avatar URL khong hop le")]
    public string? AvatarURL { get; set; }
}
