using CinemaBooking.API.Contracts.Bookings;
using CinemaBooking.Application.Bookings;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("seat-holds")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> HoldSeats(
        [FromBody] HoldSeatsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        var result = await _bookingService.HoldSeatsAsync(
            userId, request.ShowtimeId, request.SeatIds, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "One or more seats are already booked or being held")
                return Conflict(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new HoldSeatsResponse(result.HoldIds!, result.ExpiresAt!.Value));
    }

    [HttpPost("bookings")]
    [Authorize]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!User.IsInRole(Roles.Customer) && !User.IsInRole(Roles.Staff))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                message = "You do not have permission to create a booking."
            });
        }

        var userId = GetCurrentUserId();
        var isStaff = User.IsInRole(Roles.Staff);
        var customerId = isStaff ? request.CustomerId : userId;

        var fnbItems = request.FnbItems
            .Select(item => new BookingFnBItemDto(item.ItemId, item.Quantity))
            .ToList();

        var result = await _bookingService.CreateBookingAsync(
            userId, customerId, isStaff, request.ShowtimeId, request.SeatIds, fnbItems,
            request.VoucherCode, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(MapToResponse(result.Booking!));
    }

    [HttpGet("bookings/{id}")]
    public async Task<IActionResult> GetBookingById(
        int id,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id, cancellationToken);

        if (booking is null)
            return NotFound(new { success = false, message = "Booking not found." });

        var currentUserId = GetCurrentUserId();
        if (booking.UserID != currentUserId && !User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Staff))
            return Forbid();

        return Ok(MapToResponse(booking));
    }

    [HttpGet("bookings/my")]
    public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var bookings = await _bookingService.GetMyBookingsAsync(userId, cancellationToken);

        return Ok(bookings.Select(MapToResponse));
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst("userId")!.Value);
    }

    private static BookingResponse MapToResponse(Booking booking)
    {
        return new BookingResponse(
            booking.BookingID,
            booking.BookingCode,
            booking.ShowtimeID,
            booking.Showtime.Movie.Title,
            booking.Showtime.StartTime,
            booking.Showtime.Room.Cinema.CinemaName,
            booking.Showtime.Room.RoomName,
            booking.SubTotal,
            booking.DiscountAmount,
            booking.FinalAmount,
            booking.Status,
            booking.BookingDate,
            booking.BookingSeats.Select(bs => new BookingSeatResponse(
                bs.SeatID, bs.Seat.SeatRow, bs.Seat.SeatCol, bs.TicketPrice
            )).ToList(),
            booking.BookingFnBs.Select(fnb => new BookingFnBResponse(
                fnb.Product.ItemName,
                fnb.Quantity,
                fnb.UnitPrice,
                fnb.SubTotal
            )).ToList(),
            booking.BookingVoucher != null
                ? new BookingVoucherResponse(
                    booking.BookingVoucher.Voucher.VoucherCode,
                    booking.BookingVoucher.DiscountApplied
                  )
                : null
        );
    }
}
