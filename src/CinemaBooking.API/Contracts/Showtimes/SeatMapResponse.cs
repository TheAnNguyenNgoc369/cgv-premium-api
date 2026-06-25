namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record SeatMapResponse(
    int ShowtimeID, string RoomName, string RoomType, List<SeatResponse> Seats);
