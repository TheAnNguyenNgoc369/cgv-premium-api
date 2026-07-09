namespace CinemaBooking.Application.Reports;

public sealed record RevenueSummary(decimal GrossRevenue, decimal TicketRevenue, decimal FnbRevenue,
    decimal DiscountAmount, int BookingCount, int TicketsSold, decimal AverageOrderValue);
public sealed record MoviePerformance(int MovieId, string Title, int ShowtimeCount, int BookingCount,
    int TicketsSold, decimal OccupancyRate, decimal Revenue);
public sealed record ReportFile(byte[] Content, string ContentType, string FileName);
public sealed record RevenueDetail(int PaymentId, string BookingCode, DateTimeOffset PaidAt, string Cinema,
    string PaymentMethod, decimal Amount);
public sealed record FnbDetail(string BookingCode, DateTimeOffset PaidAt, string Cinema, string Product,
    int Quantity, decimal UnitPrice, decimal Subtotal);
public sealed record OccupancyDetail(int ShowtimeId, string Movie, string Cinema, string Room,
    DateTimeOffset StartTime, int Capacity, int TicketsSold, decimal OccupancyRate);
