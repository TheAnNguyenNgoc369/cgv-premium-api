namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatMapResponse(
    int RoomId,
    IReadOnlyCollection<SeatMapRow> Rows
);

public sealed record SeatMapRow(
    string RowLabel,
    IReadOnlyCollection<SeatResponse> Seats
);
