using CinemaBooking.API.Contracts.CheckIns;
using CinemaBooking.Application.CheckIns;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/checkins")]
public sealed class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInsController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    [HttpPost("lookup")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Lookup(
        [FromBody] CheckInLookupRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Staff))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        if (!TryGetCurrentUserId(out var staffId))
            return Unauthorized();

        var result = await _checkInService.LookupAsync(
            request.QRCode,
            staffId,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Booking not found.")
                return NotFound(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = new CheckInLookupResponse
        {
            BookingId = result.Data!.BookingId,
            BookingCode = result.Data.BookingCode,
            CustomerName = result.Data.CustomerName,
            Movie = new CheckInLookupResponse.MovieInfo
            {
                Title = result.Data.Movie.Title,
                Rating = result.Data.Movie.Rating,
                Duration = result.Data.Movie.Duration,
                PosterURL = result.Data.Movie.PosterURL
            },
            Cinema = new CheckInLookupResponse.CinemaInfo
            {
                Name = result.Data.Cinema.Name,
                Address = result.Data.Cinema.Address
            },
            Room = new CheckInLookupResponse.RoomInfo
            {
                Name = result.Data.Room.Name,
                RoomType = result.Data.Room.RoomType
            },
            Showtime = new CheckInLookupResponse.ShowtimeInfo
            {
                StartTime = result.Data.Showtime.StartTime,
                EndTime = result.Data.Showtime.EndTime
            },
            PaymentStatus = result.Data.PaymentStatus,
            BookingStatus = result.Data.BookingStatus,
            CheckedIn = result.Data.CheckedIn,
            Seats = result.Data.Seats.Select(s => new CheckInLookupResponse.SeatInfo
            {
                Row = s.Row,
                Column = s.Column,
                SeatType = s.SeatType,
                TicketPrice = s.TicketPrice,
                IsCheckedIn = s.IsCheckedIn,
                CheckedInAt = s.CheckedInAt
            }).ToList(),
            Products = result.Data.Products.Select(p => new CheckInLookupResponse.ProductInfo
            {
                ProductName = p.ProductName,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice,
                Subtotal = p.Subtotal
            }).ToList(),
            IsFromOtherCinema = result.Data.IsFromOtherCinema,
            WarningMessage = result.Data.WarningMessage
        };

        return Ok(new { success = true, data = response });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Staff))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        if (!TryGetCurrentUserId(out var staffId))
            return Unauthorized();
        var ipAddress = GetClientIpAddress();

        var result = await _checkInService.CheckInAsync(
            request.QRCode,
            staffId,
            ipAddress,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Booking not found.")
                return NotFound(new { success = false, message = result.ErrorMessage });

            if (result.ErrorMessage == "You cannot check in tickets from another cinema.")
                return StatusCode(403, new { success = false, message = result.ErrorMessage });

            if (result.ErrorMessage == "Ticket has already been checked in.")
                return Conflict(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new
        {
            success = true,
            message = "Ticket checked in successfully.",
            bookingCode = result.BookingCode,
            checkedInAt = result.CheckedInAt
        });
    }

    [HttpPost("fnb-pickup")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> ConfirmFnBPickup(
        [FromBody] FnBPickupRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Staff))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        if (!TryGetCurrentUserId(out var staffId))
            return Unauthorized();

        var result = await _checkInService.ConfirmFnBPickupAsync(
            request.BookingCode,
            staffId,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, message = result.ErrorMessage });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdValue = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out userId);
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    [HttpGet("history")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Manager},{Roles.Admin}")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] CheckInHistoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Staff)
            && !User.IsInRole(Roles.Manager)
            && !User.IsInRole(Roles.Admin))
            return Forbid();

        if (!ModelState.IsValid)
        {
            var error = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => entry.Value!.Errors[0].ErrorMessage)
                .FirstOrDefault() ?? "Invalid request.";

            return BadRequest(new { success = false, message = error });
        }

        if (!TryGetCurrentUserId(out var staffId))
            return Unauthorized();

        var result = await _checkInService.GetHistoryAsync(
            request.StaffId,
            request.CinemaId,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            staffId,
            User.IsInRole(Roles.Admin),
            User.IsInRole(Roles.Manager),
            User.IsInRole(Roles.Staff),
            cancellationToken);

        var response = new CheckInHistoryResponse
        {
            Records = result.Records.Select(r => new CheckInHistoryResponse.CheckInRecord
            {
                BookingId = r.BookingId,
                BookingCode = r.BookingCode,
                CustomerName = r.CustomerName,
                MovieTitle = r.MovieTitle,
                CinemaName = r.CinemaName,
                RoomName = r.RoomName,
                ShowtimeStart = r.ShowtimeStart,
                CheckedInAt = r.CheckedInAt,
                StaffName = r.StaffName,
                SeatCount = r.SeatCount,
                TotalAmount = r.TotalAmount,
                CheckedInSeats = r.CheckedInSeats.Select(s => new CheckedInSeatDto
                {
                    SeatCode = s.SeatCode,
                    SeatType = s.SeatType,
                    TicketPrice = s.TicketPrice,
                    CheckedInAt = s.CheckedInAt
                }).ToList()
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Ok(new { success = true, data = response });
    }

    [HttpGet("fnb-pickup-history")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Manager},{Roles.Admin}")]
    public async Task<IActionResult> GetFnBPickupHistory(
        [FromQuery] FnBPickupHistoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Staff)
            && !User.IsInRole(Roles.Manager)
            && !User.IsInRole(Roles.Admin))
            return Forbid();

        if (!ModelState.IsValid)
        {
            var error = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => entry.Value!.Errors[0].ErrorMessage)
                .FirstOrDefault() ?? "Invalid request.";

            return BadRequest(new { success = false, message = error });
        }

        if (!TryGetCurrentUserId(out var staffId))
            return Unauthorized();

        var result = await _checkInService.GetFnBPickupHistoryAsync(
            request.StaffId,
            request.CinemaId,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            staffId,
            User.IsInRole(Roles.Admin),
            User.IsInRole(Roles.Manager),
            User.IsInRole(Roles.Staff),
            cancellationToken);

        var response = new FnBPickupHistoryResponse
        {
            Records = result.Records.Select(r => new FnBPickupHistoryResponse.FnBPickupRecord
            {
                BookingId = r.BookingId,
                BookingCode = r.BookingCode,
                CustomerName = r.CustomerName,
                CinemaName = r.CinemaName,
                PickedUpAt = r.PickedUpAt,
                StaffName = r.StaffName,
                TotalAmount = r.TotalAmount,
                Items = r.Items.Select(i => new FnBPickupHistoryResponse.FnBPickupItem
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    SubTotal = i.SubTotal
                }).ToList()
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Ok(new { success = true, data = response });
    }
}
