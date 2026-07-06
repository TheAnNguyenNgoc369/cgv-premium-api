namespace CinemaBooking.API.Contracts.Rooms;

public sealed record RoomResponse(
    int RoomId,
    int CinemaId,
    string Name,
    int RoomTypeId,
    string Type,
    int Capacity,
    string Status,
    string? Description,
    DateTime CreatedAt
);
