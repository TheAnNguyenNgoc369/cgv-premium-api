using CinemaBooking.API.Contracts.CheckIns;
using CinemaBooking.Application.CheckIns;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/checkins")]
[Authorize(Roles = Roles.Staff)]
public sealed class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInsController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    [HttpPost("lookup")]
    public async Task<IActionResult> Lookup(
        [FromBody] CheckInLookupRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var staffId = GetCurrentUserId();
        var result = await _checkInService.LookupAsync(
            request.QRCode,
            staffId,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Booking not found.")
                return NotFound(new { success = false, message = result.ErrorMessage });

            if (result.ErrorMessage == "You cannot check in tickets from another cinema.")
                return StatusCode(403, new { success = false, message = result.ErrorMessage });

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
                TicketPrice = s.TicketPrice
            }).ToList(),
            Products = result.Data.Products.Select(p => new CheckInLookupResponse.ProductInfo
            {
                ProductName = p.ProductName,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice,
                Subtotal = p.Subtotal
            }).ToList()
        };

        return Ok(new { success = true, data = response });
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var staffId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        var result = await _checkInService.CheckInAsync(
            request.BookingId,
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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
