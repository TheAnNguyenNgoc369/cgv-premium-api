namespace CinemaBooking.API.Contracts.Showtimes;

public sealed record ShowtimeListResponse(
    int ShowtimeID,
    DateTime StartTime,
    DateTime EndTime,
    decimal BasePrice,
    int RoomID,
    string RoomName,
    string RoomType,
    int CinemaID,
    string CinemaName
);

public sealed record ShowtimeDetailResponse(
    int ShowtimeID,
    DateTime StartTime,
    DateTime EndTime,
    decimal BasePrice,
    string Status,
    int MovieID,
    string MovieTitle,
    string? PosterURL,
    int DurationMin,
    string AgeRating,
    int RoomID,
    string RoomName,
    string RoomType,
    int CinemaID,
    string CinemaName,
    string CinemaAddress
);

public sealed record SeatMapResponse(
    int ShowtimeID,
    string RoomName,
    string RoomType,
    List<SeatResponse> Seats
);

public sealed record SeatResponse(
    int SeatID,
    string SeatRow,
    int SeatCol,
    string SeatType,
    decimal ExtraPrice,
    decimal Price,
    string Status
);