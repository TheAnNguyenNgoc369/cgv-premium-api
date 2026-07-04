namespace CinemaBooking.API.Contracts.Seats;

public sealed record SeatGenerateRequest(
    int Rows,
    int Columns,
    int SeatTypeId,
    string Status
);
