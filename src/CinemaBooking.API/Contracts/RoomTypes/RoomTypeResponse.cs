namespace CinemaBooking.API.Contracts.RoomTypes;
public sealed record RoomTypeResponse(int RoomTypeId, string TypeName, decimal ExtraPrice, string? Description);
