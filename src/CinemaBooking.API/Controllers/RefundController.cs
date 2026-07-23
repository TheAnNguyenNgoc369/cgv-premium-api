using CinemaBooking.API.Contracts.Refunds;
using CinemaBooking.Application.Refunds;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class RefundController : ControllerBase
{
    private readonly IRefundService _refundService;

    public RefundController(IRefundService refundService)
    {
        _refundService = refundService;
    }

    [HttpPost("refunds")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> CreateRefund(
        [FromBody] CreateRefundRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var isStaff = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);

        var result = await _refundService.ProcessRefundAsync(
            request.BookingId,
            request.Reason,
            userId,
            isStaff,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Booking not found.")
                return NotFound(new { success = false, message = result.ErrorMessage });

            if (result.ErrorMessage == "Forbidden.")
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new RefundResponse(
            true,
            result.Result!.RefundAmount,
            result.Result.WalletBalance,
            result.Result.Status
        ));
    }

    [HttpGet("refunds")]
    public async Task<IActionResult> GetRefundHistory(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var isStaffOrAdmin = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);

        var refunds = await _refundService.GetRefundHistoryAsync(
            userId,
            isStaffOrAdmin,
            cancellationToken);

        return Ok(refunds.Select(MapToDetailResponse));
    }

    [HttpGet("refunds/{id}")]
    public async Task<IActionResult> GetRefundById(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var isStaffOrAdmin = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);

        var refund = await _refundService.GetRefundByIdAsync(
            id,
            userId,
            isStaffOrAdmin,
            cancellationToken);

        if (refund is null)
            return NotFound(new { success = false, message = "Refund not found." });

        return Ok(MapToDetailResponse(refund));
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdValue = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out userId);
    }

    private static RefundDetailResponse MapToDetailResponse(Refund refund)
    {
        var showtime = refund.Booking.Showtime;
        var room = showtime?.Room;
        var cinema = room?.Cinema;
        var movie = showtime?.Movie;

        return new RefundDetailResponse(
            refund.RefundID,
            refund.BookingID,
            refund.Booking.BookingCode,
            movie?.Title ?? "F&B Only",
            showtime?.StartTime ?? DateTime.MinValue,
            cinema?.CinemaName ?? "N/A",
            room?.RoomName ?? "N/A",
            refund.Amount,
            refund.Reason ?? string.Empty,
            refund.Status,
            refund.RequestedAt,
            refund.CompletedAt,
            refund.ProcessedByUser?.FullName
        );
    }
}
