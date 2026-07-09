namespace CinemaBooking.API.Contracts.SeatTypes;

public sealed record SeatTypeResponse(
    int SeatTypeId,
    string TypeName,
    int Capacity,
    decimal ExtraPrice);
