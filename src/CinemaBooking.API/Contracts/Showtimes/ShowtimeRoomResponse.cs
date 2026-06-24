namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeRoomResponse(
    int RoomId, string RoomName, string RoomType, int Capacity);
