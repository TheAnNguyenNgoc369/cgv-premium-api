using CinemaBooking.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public sealed class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var userId = GetCurrentUserId();

        var result = await _reviewService.CreateAsync(
            userId,
            request.BookingId,
            request.Rating,
            request.Comment,
            cancellationToken);

        if (result.Succeeded)
        {
            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                message = "Review created."
            });
        }

        return result.ErrorCode switch
        {
            "not_found" => NotFound(new { success = false, message = result.ErrorMessage }),
            "forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = result.ErrorMessage }),
            "conflict" => Conflict(new { success = false, message = result.ErrorMessage }),
            _ => BadRequest(new { success = false, message = result.ErrorMessage })
        };
    }

    [HttpGet("~/api/movies/{movieId:int}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetForMovie(
        int movieId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.GetMovieReviewsAsync(movieId, page, pageSize, cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorCode switch
            {
                "not_found" => NotFound(new { success = false, message = result.ErrorMessage }),
                _ => BadRequest(new { success = false, message = result.ErrorMessage })
            };
        }

        var data = result.Data!;

        var breakdown = new Dictionary<string, int>
        {
            ["5"] = data.RatingBreakdown.TryGetValue(5, out var r5) ? r5 : 0,
            ["4"] = data.RatingBreakdown.TryGetValue(4, out var r4) ? r4 : 0,
            ["3"] = data.RatingBreakdown.TryGetValue(3, out var r3) ? r3 : 0,
            ["2"] = data.RatingBreakdown.TryGetValue(2, out var r2) ? r2 : 0,
            ["1"] = data.RatingBreakdown.TryGetValue(1, out var r1) ? r1 : 0
        };

        return Ok(new
        {
            movieId = data.MovieId,
            averageRating = data.AverageRating,
            totalReviews = data.TotalReviews,
            ratingBreakdown = breakdown,
            items = data.Items.Select(i => new
            {
                reviewId = i.ReviewId,
                rating = i.Rating,
                comment = i.Comment,
                createdAt = i.CreatedAt,
                user = new
                {
                    id = i.UserId,
                    name = i.UserFullName,
                    avatarUrl = i.UserAvatarUrl
                }
            }),
            page = data.Page,
            pageSize = data.PageSize
        });
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst("userId")!.Value);
    }
}

public class CreateReviewRequest
{
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
