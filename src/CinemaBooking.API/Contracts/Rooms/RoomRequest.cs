namespace CinemaBooking.API.Contracts.Rooms;

public sealed record RoomRequest(
    int CinemaId,
    string Name,
    string Type,
    int Capacity,
    string Status,
    string? Description
);
