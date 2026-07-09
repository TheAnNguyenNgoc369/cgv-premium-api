namespace CinemaBooking.Application.Reports;

public sealed record DailyRevenue(DateOnly Date, decimal Revenue, decimal TicketRevenue,
    decimal FnbRevenue, int BookingCount, int TicketsSold);
