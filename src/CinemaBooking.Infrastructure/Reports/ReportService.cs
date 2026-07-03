using ClosedXML.Excel;
using CinemaBooking.Application.Reports;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Shared.Time;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CinemaBooking.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    private readonly CinemaBookingDbContext _db;
    public ReportService(CinemaBookingDbContext db) { _db = db; QuestPDF.Settings.License = LicenseType.Community; }

    private IQueryable<Payment> Payments(DateTime from, DateTime to, int? cinemaId)
    {
        var query = _db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Completed
            && p.PaidAt >= from && p.PaidAt < to && p.Booking.Status != BookingStatus.Cancelled
            && p.Booking.Status != BookingStatus.Refunded);
        return cinemaId.HasValue ? query.Where(p => p.Booking.Showtime.Room.CinemaID == cinemaId) : query;
    }

    public async Task<RevenueSummary> RevenueSummaryAsync(DateTime from, DateTime to, int? cinemaId, int actorId, string? ip, CancellationToken ct)
    {
        var payments = await Payments(from, to, cinemaId).Include(p => p.Booking).ThenInclude(b => b.BookingSeats)
            .Include(p => p.Booking).ThenInclude(b => b.BookingFnBs).ToListAsync(ct);
        var gross = payments.Sum(p => p.Amount); var bookings = payments.Select(p => p.Booking).DistinctBy(b => b.BookingID).ToList();
        var result = new RevenueSummary(gross, bookings.Sum(b => b.BookingSeats.Sum(s => s.TicketPrice)),
            bookings.Sum(b => b.BookingFnBs.Sum(f => f.SubTotal)), bookings.Sum(b => b.DiscountAmount),
            bookings.Count, bookings.Sum(b => b.BookingSeats.Count), bookings.Count == 0 ? 0 : gross / bookings.Count);
        await Audit(actorId, AdminActionTypes.ViewRevenueReport, "Viewed revenue summary", ip, ct); return result;
    }

    public async Task<IReadOnlyList<MoviePerformance>> MoviePerformanceAsync(DateTime from, DateTime to, string? search,
        int? cinemaId, int actorId, string? ip, CancellationToken ct)
    {
        IQueryable<Payment> payments = Payments(from, to, cinemaId).Include(p => p.Booking).ThenInclude(b => b.BookingSeats)
            .Include(p => p.Booking).ThenInclude(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(p => p.Booking).ThenInclude(b => b.Showtime).ThenInclude(s => s.Room);
        if (!string.IsNullOrWhiteSpace(search)) payments = payments.Where(p => p.Booking.Showtime.Movie.Title.Contains(search));
        var rows = await payments.ToListAsync(ct);
        var result = rows.GroupBy(p => new { p.Booking.Showtime.MovieID, p.Booking.Showtime.Movie.Title })
            .Select(g => { var bookings = g.Select(x => x.Booking).DistinctBy(x => x.BookingID).ToList();
                var shows = bookings.Select(x => x.Showtime).DistinctBy(x => x.ShowtimeID).ToList(); var sold = bookings.Sum(x => x.BookingSeats.Count);
                var capacity = shows.Sum(x => x.Room.Capacity); return new MoviePerformance(g.Key.MovieID, g.Key.Title,
                    shows.Count, bookings.Count, sold, capacity == 0 ? 0 : Math.Round(sold * 100m / capacity, 2), g.Sum(x => x.Amount)); })
            .OrderByDescending(x => x.Revenue).ToList();
        await Audit(actorId, AdminActionTypes.ViewRevenueReport, "Viewed movie performance report", ip, ct); return result;
    }

    public async Task<ReportFile> ExportAsync(string format, string type, DateTime from, DateTime to, int? cinemaId,
        int actorId, string? ip, CancellationToken ct)
    {
        string[] headers; List<string[]> rows;
        if (type == "revenue") { headers = ["Payment ID", "Booking Code", "Paid At", "Cinema", "Payment Method", "Amount"];
            rows = (await RevenueRows(from, to, cinemaId, ct)).Select(x => new[] { x.PaymentId.ToString(), x.BookingCode, x.PaidAt.ToString("O"), x.Cinema, x.PaymentMethod, x.Amount.ToString("0.00") }).ToList(); }
        else if (type == "fnb") { headers = ["Booking Code", "Paid At", "Cinema", "Product", "Quantity", "Unit Price", "Subtotal"];
            rows = (await FnbRows(from, to, cinemaId, ct)).Select(x => new[] { x.BookingCode, x.PaidAt.ToString("O"), x.Cinema, x.Product, x.Quantity.ToString(), x.UnitPrice.ToString("0.00"), x.Subtotal.ToString("0.00") }).ToList(); }
        else { headers = ["Showtime ID", "Movie", "Cinema", "Room", "Start Time", "Capacity", "Tickets Sold", "Occupancy Rate"];
            rows = (await OccupancyRows(from, to, cinemaId, ct)).Select(x => new[] { x.ShowtimeId.ToString(), x.Movie, x.Cinema, x.Room, x.StartTime.ToString("O"), x.Capacity.ToString(), x.TicketsSold.ToString(), x.OccupancyRate.ToString("0.00") }).ToList(); }
        var extension = format == "excel" ? "xlsx" : "pdf"; var bytes = format == "excel" ? Excel(headers, rows) : Pdf(headers, rows);
        await Audit(actorId, AdminActionTypes.ExportReport, $"Exported {type} report as {format}", ip, ct);
        return new(bytes, format == "excel" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf",
            $"{type}-report-{VietnamTime.GetDate(from):yyyyMMdd}-{VietnamTime.GetDate(to.AddTicks(-1)):yyyyMMdd}.{extension}");
    }

    private async Task<List<RevenueDetail>> RevenueRows(DateTime f, DateTime t, int? c, CancellationToken ct)
    {
        var rows = await Payments(f,t,c).Select(p => new { p.PaymentID, p.Booking.BookingCode, PaidAt = p.PaidAt!.Value,
            Cinema = p.Booking.Showtime.Room.Cinema.CinemaName, p.PaymentMethod, p.Amount }).ToListAsync(ct);
        return rows.Select(p => new RevenueDetail(p.PaymentID, p.BookingCode, VietnamTime.FromUtc(p.PaidAt),
            p.Cinema, p.PaymentMethod, p.Amount)).ToList();
    }
    private async Task<List<FnbDetail>> FnbRows(DateTime f, DateTime t, int? c, CancellationToken ct)
    {
        var rows = await Payments(f,t,c).SelectMany(p => p.Booking.BookingFnBs.Select(x => new { p.Booking.BookingCode,
            PaidAt = p.PaidAt!.Value, Cinema = p.Booking.Showtime.Room.Cinema.CinemaName, Product = x.Product.ItemName,
            x.Quantity, x.UnitPrice, x.SubTotal })).ToListAsync(ct);
        return rows.Select(x => new FnbDetail(x.BookingCode, VietnamTime.FromUtc(x.PaidAt), x.Cinema,
            x.Product, x.Quantity, x.UnitPrice, x.SubTotal)).ToList();
    }
    private async Task<List<OccupancyDetail>> OccupancyRows(DateTime f, DateTime t, int? c, CancellationToken ct)
    { var data = await Payments(f,t,c).Include(p=>p.Booking).ThenInclude(b=>b.BookingSeats).Include(p=>p.Booking).ThenInclude(b=>b.Showtime).ThenInclude(s=>s.Room).ThenInclude(r=>r.Cinema).Include(p=>p.Booking).ThenInclude(b=>b.Showtime).ThenInclude(s=>s.Movie).ToListAsync(ct);
      return data.GroupBy(p=>p.Booking.ShowtimeID).Select(g=> { var sold=g.Select(x=>x.Booking).DistinctBy(x=>x.BookingID).Sum(x=>x.BookingSeats.Count); var s=g.First().Booking.Showtime; return new OccupancyDetail(s.ShowtimeID,s.Movie.Title,s.Room.Cinema.CinemaName,s.Room.RoomName,VietnamTime.FromUtc(s.StartTime),s.Room.Capacity,sold,s.Room.Capacity==0?0:Math.Round(sold*100m/s.Room.Capacity,2));}).ToList(); }

    private static byte[] Excel(string[] headers, List<string[]> rows) { using var wb=new XLWorkbook(); var ws=wb.Worksheets.Add("Report");
      for(int c=0;c<headers.Length;c++) ws.Cell(1,c+1).Value=headers[c]; for(int r=0;r<rows.Count;r++) for(int c=0;c<headers.Length;c++) ws.Cell(r+2,c+1).Value=rows[r][c]; ws.Row(1).Style.Font.Bold=true; ws.Columns().AdjustToContents(); using var ms=new MemoryStream(); wb.SaveAs(ms); return ms.ToArray(); }
    private static byte[] Pdf(string[] headers, List<string[]> rows) => Document.Create(doc => doc.Page(page => { page.Size(PageSizes.A4.Landscape()); page.Margin(20); page.Content().Table(table => { table.ColumnsDefinition(c => { foreach(var _ in headers)c.RelativeColumn(); }); table.Header(h => { foreach(var x in headers)h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(x).Bold(); }); foreach(var row in rows)foreach(var x in row)table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3).Text(x).FontSize(8); }); })).GeneratePdf();
    private async Task Audit(int actor, string action, string desc, string? ip, CancellationToken ct) { _db.AdminActionLogs.Add(new AdminActionLog { AdminID=actor,TargetTable="Reports",ActionType=action,Description=desc,IPAddress=ip,CreatedAt=DateTime.UtcNow}); await _db.SaveChangesAsync(ct); }
}
