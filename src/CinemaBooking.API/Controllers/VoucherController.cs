using CinemaBooking.API.Contracts.Vouchers;
using CinemaBooking.Application.Vouchers;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Shared.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController, Route("api/vouchers")]
public sealed class VoucherController : ControllerBase
{
    private readonly IVoucherService _service;
    public VoucherController(IVoucherService service) => _service = service;

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? searchKeyword = null, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAsync(searchKeyword, pageIndex, pageSize, cancellationToken);
        var currentTime = DateTime.UtcNow;
        return Ok(new VoucherPageResponse(result.Items.Select(v => Map(v, currentTime)).ToList(), result.PageIndex, result.PageSize,
            result.TotalItems, (int)Math.Ceiling(result.TotalItems / (double)result.PageSize)));
    }

    [HttpPost, Authorize(Roles = Roles.Admin), Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] VoucherRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var id)) return Unauthorized();
        await using var stream = request.Image?.OpenReadStream();
        var result = await _service.CreateAsync(id, Command(request), stream, request.Image?.FileName,
            request.Image?.ContentType, request.Image?.Length ?? 0, Ip(), ct);
        if (!result.Succeeded) return Error(result);
        return Created($"/api/vouchers/{result.Voucher!.VoucherID}", Map(result.Voucher));
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin), Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] VoucherRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var adminId)) return Unauthorized();
        await using var stream = request.Image?.OpenReadStream();
        var result = await _service.UpdateAsync(adminId, id, Command(request), stream, request.Image?.FileName,
            request.Image?.ContentType, request.Image?.Length ?? 0, Ip(), ct);
        return result.Succeeded ? Ok(Map(result.Voucher!)) : Error(result);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!TryUserId(out var adminId)) return Unauthorized();
        var result = await _service.DeleteAsync(adminId, id, Ip(), ct);
        return result.Succeeded ? NoContent() : Error(result);
    }

    private IActionResult Error(VoucherResult r) => r.ErrorType switch
    { "not_found" => NotFound(new { success = false, message = r.Error }), "conflict" => Conflict(new { success = false, message = r.Error }), _ => BadRequest(new { success = false, message = r.Error }) };
    private static VoucherCommand Command(VoucherRequest r) => new(r.VoucherCode, r.Category, r.DiscountType,
        r.DiscountValue, r.MinOrderValue, r.MaxUses, r.ValidFrom!.Value, r.ValidUntil!.Value, r.Description, r.IsActive);
    private static VoucherResponse Map(Voucher v) => Map(v, DateTime.UtcNow);
    private static VoucherResponse Map(Voucher v, DateTime currentTime) => new(v.VoucherID, v.VoucherCode, v.Category, v.DiscountType,
        v.DiscountValue, v.MinOrderValue, v.MaxUses, v.UsedCount, VietnamTime.FromUtc(v.ValidFrom),
        VietnamTime.FromUtc(v.ValidUntil), v.ImageURL, v.Description, v.IsActive, Status(v, currentTime), v.CreatedAt);
    private static string Status(Voucher v, DateTime currentTime)
    {
        if (!v.IsActive) return "DISABLED";
        if (currentTime > v.ValidUntil) return "EXPIRED";
        if (v.MaxUses.HasValue && v.UsedCount >= v.MaxUses.Value) return "EXHAUSTED";
        if (currentTime < v.ValidFrom) return "UPCOMING";
        return "ACTIVE";
    }
    private bool TryUserId(out int id) => int.TryParse(User.FindFirst("userId")?.Value, out id);
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
