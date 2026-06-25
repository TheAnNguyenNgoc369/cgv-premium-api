using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.SeatTypes;

public sealed class SeatTypeRequest
{
    [Required(ErrorMessage = "Please enter the seat type name")]
    [MaxLength(20, ErrorMessage = "Seat type name must not exceed 20 characters")]
    public string TypeName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ErrorMessage = "Extra price must be greater than or equal to 0")]
    public decimal ExtraPrice { get; set; }
}
