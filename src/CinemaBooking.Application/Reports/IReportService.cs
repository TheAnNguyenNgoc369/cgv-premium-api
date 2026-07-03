namespace CinemaBooking.Application.Reports;

public interface IReportService
{
    Task<RevenueSummary> RevenueSummaryAsync(DateTime fromUtc, DateTime toUtc, int? cinemaId, int actorId, string? ip, CancellationToken ct);
    Task<IReadOnlyList<MoviePerformance>> MoviePerformanceAsync(DateTime fromUtc, DateTime toUtc, string? search, int? cinemaId, int actorId, string? ip, CancellationToken ct);
    Task<ReportFile> ExportAsync(string format, string reportType, DateTime fromUtc, DateTime toUtc, int? cinemaId, int actorId, string? ip, CancellationToken ct);
}
