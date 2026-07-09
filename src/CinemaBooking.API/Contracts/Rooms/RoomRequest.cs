namespace CinemaBooking.API.Contracts.Rooms;

public sealed record RoomRequest(
    int CinemaId,
    string Name,
    int RoomTypeId,
    string Status,
    string? Description
);
