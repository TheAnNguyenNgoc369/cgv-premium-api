namespace CinemaBooking.Application.Reports;

public sealed record WeeklyRevenue(string Week, DateOnly StartDate, DateOnly EndDate, decimal Revenue,
    decimal TicketRevenue, decimal FnbRevenue, int BookingCount, int TicketsSold);
