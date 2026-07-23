using CinemaBooking.API.Contracts.Membership;
using CinemaBooking.Application.Membership;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/membership")]
[Authorize]
public sealed class MembershipController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyMembership(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var membership = await _membershipService.GetMyMembershipAsync(userId, cancellationToken);

        var response = new MembershipResponse(
            CurrentTier: membership.CurrentTier,
            NextTier: membership.NextTier,
            PointsToNextTier: membership.PointsToNextTier,
            TotalPoints: membership.TotalPoints,
            TotalSpent: membership.TotalSpent,
            DiscountPercent: membership.DiscountPercent,
            TotalRefunds: membership.TotalRefunds,
            UsedRefunds: membership.UsedRefunds
        );

        return Ok(response);
    }

    [HttpGet("tiers")]
    public async Task<IActionResult> GetTiers(CancellationToken cancellationToken)
    {
        var tiers = await _membershipService.GetTiersAsync(cancellationToken);

        var response = tiers.Select(t => new TierResponse(
            TierID: t.TierID,
            TierName: t.TierName,
            MinPoints: t.MinPoints,
            DiscountRate: t.DiscountRate,
            TotalRefunds: t.MaxRefundPerMonth
        )).ToList();

        return Ok(response);
    }

    [HttpPost("tiers")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateTier(
        [FromBody] CreateTierRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var result = await _membershipService.CreateTierAsync(
            request.TierName,
            request.MinPoints,
            request.DiscountRate,
            request.MaxRefundPerMonth,
            cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        var tier = result.Tier!;
        var response = new TierResponse(
            TierID: tier.TierID,
            TierName: tier.TierName,
            MinPoints: tier.MinPoints,
            DiscountRate: tier.DiscountRate,
            TotalRefunds: tier.MaxRefundPerMonth
        );

        return CreatedAtAction(nameof(GetTiers), null, response);
    }

    [HttpGet("points-history")]
    public async Task<IActionResult> GetPointsHistory(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var history = await _membershipService.GetPointHistoryAsync(userId, cancellationToken);

        var response = history.Select(h => new PointHistoryResponse(
            PointsDelta: h.PointsDelta,
            TransactionType: h.TransactionType,
            Description: h.Description,
            CreatedAt: h.CreatedAt
        )).ToList();

        return Ok(response);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdValue = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out userId);
    }
}
