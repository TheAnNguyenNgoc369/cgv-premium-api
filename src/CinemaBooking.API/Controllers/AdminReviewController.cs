using CinemaBooking.Application.Reviews;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public AdminReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPatch("{reviewId:int}/hide")]
    public async Task<IActionResult> Hide(int reviewId, CancellationToken cancellationToken)
    {
        var adminId = GetCurrentUserId();
        var result = await _reviewService.HideAsync(reviewId, adminId, cancellationToken);

        if (result.Succeeded)
        {
            return Ok(new { success = true, message = "Review hidden." });
        }

        return result.ErrorCode switch
        {
            "not_found" => NotFound(new { success = false, message = result.ErrorMessage }),
            _ => BadRequest(new { success = false, message = result.ErrorMessage })
        };
    }

    [HttpPatch("{reviewId:int}/unhide")]
    public async Task<IActionResult> Unhide(int reviewId, CancellationToken cancellationToken)
    {
        var result = await _reviewService.UnhideAsync(reviewId, cancellationToken);

        if (result.Succeeded)
        {
            return Ok(new { success = true, message = "Review unhidden." });
        }

        return result.ErrorCode switch
        {
            "not_found" => NotFound(new { success = false, message = result.ErrorMessage }),
            _ => BadRequest(new { success = false, message = result.ErrorMessage })
        };
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst("userId")!.Value);
    }
}
