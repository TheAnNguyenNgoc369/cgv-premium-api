namespace CinemaBooking.API.Contracts.Tickets;

public sealed record TicketResponse(
    int TicketID,
    string QRCode,
    string Status,
    int BookingSeatID,
    int SeatID,
    string SeatRow,
    int SeatCol
);
