using CinemaBooking.API.Contracts.LoyaltyTiers;
using CinemaBooking.Application.LoyaltyTiers;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/admin/loyalty-tiers")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminLoyaltyTierController(ILoyaltyTierService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tiers = await service.GetAllAsync(cancellationToken);
        return Ok(tiers.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var tier = await service.GetByIdAsync(id, cancellationToken);

        return tier is null
            ? NotFound(new { success = false, message = "Loyalty tier not found." })
            : Ok(ToResponse(tier));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] LoyaltyTierRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var result = await service.CreateAsync(
            request.TierName,
            request.MinPoints,
            request.DiscountRate,
            request.MaxRefundPerMonth,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToFailureResponse(result);
        }

        var response = ToResponse(result.Tier!);
        return CreatedAtAction(nameof(GetById), new { id = response.TierID }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] LoyaltyTierRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var result = await service.UpdateAsync(
            id,
            request.TierName,
            request.MinPoints,
            request.DiscountRate,
            request.MaxRefundPerMonth,
            cancellationToken);

        return result.Succeeded
            ? Ok(ToResponse(result.Tier!))
            : ToFailureResponse(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);

        if (result.Succeeded)
        {
            return NoContent();
        }

        if (result.IsNotFound)
        {
            return NotFound(new { success = false, message = result.ErrorMessage });
        }

        return Conflict(new { success = false, message = result.ErrorMessage });
    }

    private static IActionResult ToFailureResponse(LoyaltyTierResult result)
    {
        if (result.IsNotFound)
        {
            return new NotFoundObjectResult(new { success = false, message = result.ErrorMessage });
        }

        if (result.IsConflict)
        {
            return new ConflictObjectResult(new { success = false, message = result.ErrorMessage });
        }

        return new BadRequestObjectResult(new { success = false, message = result.ErrorMessage });
    }

    private static LoyaltyTierResponse ToResponse(LoyaltyTier tier)
    {
        return new LoyaltyTierResponse(
            tier.TierID,
            tier.TierName,
            tier.MinPoints,
            tier.DiscountRate,
            tier.MaxRefundPerMonth);
    }
}
