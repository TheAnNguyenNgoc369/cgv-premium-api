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

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] int? movieId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var statusFilter = (status?.Trim().ToLowerInvariant()) switch
        {
            "active" => AdminReviewStatusFilter.Active,
            "hidden" => AdminReviewStatusFilter.Hidden,
            null or "" or "all" => AdminReviewStatusFilter.All,
            _ => (AdminReviewStatusFilter?)null
        };

        if (statusFilter is null)
        {
            return BadRequest(new { success = false, message = "status must be 'active', 'hidden', or 'all'." });
        }

        var result = await _reviewService.SearchAdminReviewsAsync(
            keyword,
            movieId,
            statusFilter.Value,
            page,
            pageSize,
            cancellationToken);

        return Ok(new
        {
            items = result.Items.Select(i => new
            {
                reviewId = i.ReviewId,
                movieId = i.MovieId,
                movieTitle = i.MovieTitle,
                userId = i.UserId,
                customerName = i.CustomerName,
                customerAvatar = i.CustomerAvatar,
                rating = i.Rating,
                comment = i.Comment,
                isHidden = i.IsHidden,
                createdAt = i.CreatedAt,
                hiddenAt = i.HiddenAt
            }),
            page = result.Page,
            pageSize = result.PageSize,
            totalItems = result.TotalItems,
            totalPages = result.TotalPages
        });
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
