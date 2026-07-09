namespace CinemaBooking.Application.Movie;

public sealed record MovieTicketSales(
    int MovieId,
    string Title,
    int TicketsSold);
