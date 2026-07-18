using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/admin/review-settings")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminReviewSettingsController(
    IReviewRewardSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await service.GetSettingsAsync(cancellationToken);

        if (settings is null)
        {
            return NotFound(new { success = false, message = "Review settings not found." });
        }

        return Ok(new
        {
            firstReviewPoints = settings.FirstReviewPoints,
            nextReviewPoints = settings.NextReviewPoints
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateReviewSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        if (request.FirstReviewPoints < 0 || request.NextReviewPoints < 0)
        {
            return BadRequest(new { success = false, message = "Review points must be >= 0." });
        }

        try
        {
            var userId = int.TryParse(User.FindFirst("uid")?.Value, out var id) ? id : (int?)null;
            await service.UpdateSettingsAsync(
                request.FirstReviewPoints,
                request.NextReviewPoints,
                userId,
                cancellationToken);

            return Ok(new { success = true, message = "Review reward settings updated." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}

public class UpdateReviewSettingsRequest
{
    public int FirstReviewPoints { get; set; }
    public int NextReviewPoints { get; set; }
}
