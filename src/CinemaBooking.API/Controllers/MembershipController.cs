using CinemaBooking.API.Contracts.Membership;
using CinemaBooking.Application.Membership;
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
        var userId = GetCurrentUserId();

        var membership = await _membershipService.GetMyMembershipAsync(userId, cancellationToken);

        var response = new MembershipResponse(
            Tier: membership.Tier,
            TotalPoints: membership.TotalPoints,
            TotalSpent: membership.TotalSpent,
            DiscountPercent: membership.DiscountPercent,
            IsVip: membership.IsVip,
            PointsToVip: membership.PointsToVip
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
            DiscountRate: t.DiscountRate
        )).ToList();

        return Ok(response);
    }

    [HttpGet("points-history")]
    public async Task<IActionResult> GetPointsHistory(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var history = await _membershipService.GetPointHistoryAsync(userId, cancellationToken);

        var response = history.Select(h => new PointHistoryResponse(
            PointsDelta: h.PointsDelta,
            TransactionType: h.TransactionType,
            Description: h.Description,
            CreatedAt: h.CreatedAt
        )).ToList();

        return Ok(response);
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst("userId")!.Value);
    }
}
