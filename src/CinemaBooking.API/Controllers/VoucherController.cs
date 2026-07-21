using CinemaBooking.API.Contracts.Vouchers;
using CinemaBooking.Application.Vouchers;
using CinemaBooking.Application.Vouchers.RuleEngine.Metadata;
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
    private readonly IVoucherRuleMetadataProvider _ruleMetadata;
    private readonly UserVoucherProjection _voucherProjection;

    public VoucherController(
        IVoucherService service,
        IVoucherRuleMetadataProvider ruleMetadata,
        UserVoucherProjection voucherProjection)
    {
        _service = service;
        _ruleMetadata = ruleMetadata;
        _voucherProjection = voucherProjection;
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? searchKeyword = null, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAsync(searchKeyword, pageIndex, pageSize, cancellationToken);
        var currentTime = DateTime.UtcNow;
        return Ok(new VoucherPageResponse(result.Items.Select(v => Map(v, currentTime)).ToList(), result.PageIndex, result.PageSize,
            result.TotalItems, (int)Math.Ceiling(result.TotalItems / (double)result.PageSize)));
    }

    /// <summary>
    /// Returns UI-rendering metadata for every supported voucher rule type so
    /// the admin form can build its editor dynamically instead of hard-coding
    /// rule names or option lists.
    /// </summary>
    [HttpGet("rule-types"), Authorize(Roles = Roles.Admin)]
    public IActionResult GetRuleTypes()
    {
        var metadata = _ruleMetadata.GetAll()
            .Select(m => new VoucherRuleTypeMetadataResponse(
                m.RuleType, m.DisplayName, m.InputType, m.DataSource, m.Options))
            .ToList();
        return Ok(metadata);
    }

    /// <summary>
    /// Get all redeemable vouchers available for exchange with loyalty points
    /// </summary>
    [HttpGet("redeemable"), Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> GetRedeemable(CancellationToken ct)
    {
        var result = await _service.GetRedeemableVouchersAsync(ct);
        if (!result.Succeeded) return BadRequest(new { success = false, message = result.Error });

        var vouchers = await _voucherProjection.ProjectRedeemableAsync(result.Vouchers, ct);
        return Ok(new { success = true, vouchers });
    }

    /// <summary>
    /// Redeem a voucher using loyalty points
    /// </summary>
    [HttpPost("redeem"), Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Redeem([FromBody] RedeemVoucherRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await _service.RedeemVoucherAsync(userId, request.VoucherId, Ip(), ct);
        if (!result.Succeeded) return ErrorRedeem(result);
        return Ok(new RedeemVoucherResponse(true, result.RemainingPoints, result.VoucherCode));
    }

    /// <summary>
    /// Get all vouchers redeemed by the current user
    /// </summary>
    [HttpGet("my-vouchers"), Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> GetMyVouchers(CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await _service.GetUserVouchersAsync(userId, ct);
        if (!result.Succeeded) return BadRequest(new { success = false, message = result.Error });

        var vouchers = await _voucherProjection.ProjectMyVouchersAsync(result.Vouchers, ct);
        return Ok(new { success = true, vouchers });
    }

    [HttpPost, Authorize(Roles = Roles.Admin), Consumes("application/json")]
    public async Task<IActionResult> Create([FromBody] VoucherRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var id)) return Unauthorized();
        var result = await _service.CreateAsync(id, Command(request), Ip(), ct);
        if (!result.Succeeded) return Error(result);
        return Created($"/api/vouchers/{result.Voucher!.VoucherID}", Map(result.Voucher));
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin), Consumes("application/json")]
    public async Task<IActionResult> Update(int id, [FromBody] VoucherRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var adminId)) return Unauthorized();
        var result = await _service.UpdateAsync(adminId, id, Command(request), Ip(), ct);
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
    private IActionResult ErrorRedeem(RedeemVoucherResult r) => r.ErrorType switch
    { "not_found" => NotFound(new { success = false, message = r.Error }), "forbidden" => StatusCode(403, new { success = false, message = r.Error }), _ => BadRequest(new { success = false, message = r.Error }) };
    private static VoucherCommand Command(VoucherRequest r) => new(r.VoucherCode, r.DiscountType,
        r.DiscountValue, r.MinOrderValue, r.MaxUses, r.ValidFrom!.Value, r.ValidUntil!.Value, r.Description, r.IsActive,
        r.ImageUrl, r.ImagePublicId,
        r.Rules?.Select(rule => new VoucherRuleDto(rule.RuleType, rule.RuleValue)).ToList(),
        r.IsRedeemable, r.RequiredPoints, r.ExchangeLimit);
    private static VoucherResponse Map(Voucher v) => Map(v, DateTime.UtcNow);
    private static VoucherResponse Map(Voucher v, DateTime currentTime) => new(v.VoucherID, v.VoucherCode, v.DiscountType,
        v.DiscountValue, v.MinOrderValue, v.MaxUses, v.UsedCount, VietnamTime.FromUtc(v.ValidFrom),
        VietnamTime.FromUtc(v.ValidUntil), v.ImageURL, v.Description, v.IsActive,
        UserVoucherProjection.VoucherLifecycleStatus(v, currentTime), v.CreatedAt,
        v.VoucherRules?.Select(r => new VoucherRuleResponse(r.RuleID, r.RuleType, r.RuleValue, r.CreatedAt)).ToList(),
        v.IsRedeemable, v.RequiredPoints, v.ExchangeLimit);
    private bool TryUserId(out int id) => int.TryParse(User.FindFirst("userId")?.Value, out id);
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

}
