using System.ComponentModel.DataAnnotations;
namespace CinemaBooking.API.Contracts.RoomTypes;
public sealed record RoomTypeRequest([param: Required] string TypeName, decimal ExtraPrice, string? Description);
