using CinemaBooking.Application.Common.Security;
using CinemaBooking.Application.Reports;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Shared.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController, Route("api/v1/reports"), Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly IManagerCinemaScopeService _scope;
    public ReportsController(IReportService reports, IManagerCinemaScopeService scope) { _reports = reports; _scope = scope; }

    [HttpGet("revenue-summary")]
    public async Task<IActionResult> RevenueSummary([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate,
        [FromQuery] int? cinemaId, CancellationToken ct) => await Run(startDate, endDate, cinemaId,
            async (f,t,c,u,ip) => Ok(await _reports.RevenueSummaryAsync(f,t,c,u,ip,ct)));

    [HttpGet("movie-performance")]
    public async Task<IActionResult> MoviePerformance([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate,
        [FromQuery] string? searchMovie, [FromQuery] int? cinemaId, CancellationToken ct) => await Run(startDate,endDate,cinemaId,
            async (f,t,c,u,ip) => Ok(await _reports.MoviePerformanceAsync(f,t,searchMovie?.Trim(),c,u,ip,ct)));

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] string reportType,
        [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, [FromQuery] int? cinemaId, CancellationToken ct)
    {
        format = format?.Trim().ToLowerInvariant() ?? ""; reportType = reportType?.Trim().ToLowerInvariant() ?? "";
        if (format is not ("excel" or "pdf")) return BadRequest(new { success=false,message="Format must be excel or pdf" });
        if (reportType is not ("revenue" or "fnb" or "occupancy")) return BadRequest(new { success=false,message="ReportType must be revenue, fnb, or occupancy" });
        return await Run(startDate,endDate,cinemaId, async (f,t,c,u,ip) => { var file=await _reports.ExportAsync(format,reportType,f,t,c,u,ip,ct); return File(file.Content,file.ContentType,file.FileName); });
    }

    private async Task<IActionResult> Run(DateOnly? start, DateOnly? end, int? requestedCinema,
        Func<DateTime,DateTime,int?,int,string?,Task<IActionResult>> action)
    {
        if (!start.HasValue || !end.HasValue || start > end) return BadRequest(new { success=false,message="StartDate and EndDate are required and StartDate must not be after EndDate" });
        if (!int.TryParse(User.FindFirst("userId")?.Value,out var userId)) return Unauthorized();
        int? cinema = requestedCinema;
        if (User.IsInRole(Roles.Manager)) { var assigned=await _scope.GetAssignedCinemaIdAsync(userId); if (!assigned.HasValue) return StatusCode(403,new { success=false,message="Manager is not assigned to a cinema" });
            if (requestedCinema.HasValue && requestedCinema != assigned) return StatusCode(403,new { success=false,message="Cannot access another cinema" }); cinema=assigned; }
        var from=VietnamTime.ToUtc(start.Value,TimeOnly.MinValue); var to=VietnamTime.ToUtc(end.Value.AddDays(1),TimeOnly.MinValue);
        return await action(from,to,cinema,userId,HttpContext.Connection.RemoteIpAddress?.ToString());
    }
}
