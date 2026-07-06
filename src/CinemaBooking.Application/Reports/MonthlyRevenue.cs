namespace CinemaBooking.Application.Reports;

public sealed record MonthlyRevenue(string Month, decimal Revenue, decimal TicketRevenue,
    decimal FnbRevenue, int BookingCount, int TicketsSold);
