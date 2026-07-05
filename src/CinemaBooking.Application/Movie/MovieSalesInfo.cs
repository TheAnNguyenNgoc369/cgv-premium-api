namespace CinemaBooking.Application.Movie;

public sealed record MovieSalesInfo(
    int TicketsSold,
    bool IsTopSelling,
    int? SalesRank);
