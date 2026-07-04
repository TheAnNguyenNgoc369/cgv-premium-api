namespace CinemaBooking.Application.Reports;

public sealed record TopCinema(
    int CinemaId,
    string CinemaName,
    int BookingCount,
    int TicketsSold);
