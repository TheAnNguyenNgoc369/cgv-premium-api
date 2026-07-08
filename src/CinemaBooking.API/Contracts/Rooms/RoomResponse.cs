using CinemaBooking.API.Contracts.RoomTypes;
using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Rooms;

public sealed record RoomResponse(
    int RoomId,
    int CinemaId,
    string Name,
    [property: JsonPropertyName("room_type")] RoomTypeResponse RoomType,
    int Capacity,
    string Status,
    string? Description,
    DateTime CreatedAt
);
