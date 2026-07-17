using CinemaBooking.API.Contracts.Bookings;
using CinemaBooking.Application.Bookings;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Payments;
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
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentService _paymentService;

    public BookingController(
        IBookingService bookingService,
        IBookingRepository bookingRepository,
        IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _bookingRepository = bookingRepository;
        _paymentService = paymentService;
    }

    [HttpPost("seat-holds")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> HoldSeats(
        [FromBody] HoldSeatsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        var userId = GetCurrentUserId();

        var result = await _bookingService.HoldSeatsAsync(
            userId, request.ShowtimeId, request.SeatIds, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.SeatErrors is not null)
                return BadRequest(new { success = false, message = result.ErrorMessage, errors = result.SeatErrors });

            if (result.ErrorMessage == "Showtime not found.")
                return NotFound(new { success = false, message = result.ErrorMessage });

            if (result.ErrorMessage == "One or more seats are already booked or being held."
                || result.ErrorMessage?.StartsWith("Seats with IDs ", StringComparison.Ordinal) == true)
                return Conflict(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new HoldSeatsResponse(result.HoldIds!, result.ExpiresAt!.Value));
    }

    [HttpDelete("seat-holds")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> ReleaseSeatHolds(
        [FromBody] ReleaseSeatHoldsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        var result = await _bookingService.ReleaseSeatHoldsAsync(
            GetCurrentUserId(), request.ShowtimeId, request.SeatIds, cancellationToken);
        if (!result.Succeeded)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, message = "Seat holds released successfully." });
    }

    [HttpPost("bookings/calculate-pricing")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> CalculatePricing(
        [FromBody] CalculatePricingRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        var currentUserId = GetCurrentUserId();
        var isStaff = User.IsInRole(Roles.Staff);
        var userId = isStaff ? request.CustomerId : currentUserId;

        var fnbItems = request.FnbItems
            .Select(item => new BookingFnBItemDto(item.ItemId, item.Quantity))
            .ToList();

        var result = await _bookingService.CalculatePricingAsync(
            userId, request.ShowtimeId, request.SeatIds, fnbItems,
            request.VoucherCode, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(MapToPricingResponse(result.Result!));
    }

    [HttpPost("bookings")]
    [Authorize]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

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
        {
            if (result.SeatErrors is not null)
                return BadRequest(new { success = false, message = result.ErrorMessage, errors = result.SeatErrors });

            if (result.ErrorMessage == "Showtime not found.")
                return NotFound(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

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

        if (User.IsInRole(Roles.Staff))
        {
            var staffCinemaId = await _bookingRepository.GetStaffCinemaIdAsync(
                currentUserId, cancellationToken);
            if (!staffCinemaId.HasValue
                || booking.Showtime.Room.CinemaID != staffCinemaId.Value)
                return Forbid();
        }

        booking = await SynchronizePendingPayOSBookingAsync(
            booking, currentUserId, User.IsInRole(Roles.Staff), cancellationToken);

        return Ok(MapToResponse(booking));
    }

    [HttpGet("bookings/my")]
    public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var bookings = await _bookingService.GetMyBookingsAsync(userId, cancellationToken);
        var synchronized = new List<Booking>(bookings.Count);

        foreach (var booking in bookings)
        {
            synchronized.Add(await SynchronizePendingPayOSBookingAsync(
                booking, userId, isStaff: false, cancellationToken));
        }

        return Ok(synchronized.Select(MapToMyResponse));
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst("userId")!.Value);
    }

    private async Task<Booking> SynchronizePendingPayOSBookingAsync(
        Booking booking,
        int currentUserId,
        bool isStaff,
        CancellationToken cancellationToken)
    {
        if (booking.Status != BookingStatus.Pending)
            return booking;

        var result = await _paymentService.GetPaymentByBookingIdAsync(
            booking.BookingID, currentUserId, isStaff, cancellationToken);
        if (!result.Succeeded)
            return booking;

        return await _bookingService.GetBookingByIdAsync(booking.BookingID, cancellationToken)
            ?? booking;
    }

    private static BookingResponse MapToResponse(Booking booking)
    {
        return new BookingResponse(
            booking.BookingID,
            booking.BookingCode,
            booking.ShowtimeID,
            booking.Showtime?.Movie.Title ?? "F&B Only",
            booking.Showtime?.StartTime ?? DateTime.MinValue,
            booking.Showtime?.Room.Cinema.CinemaName ?? "N/A",
            booking.Showtime?.Room.RoomName ?? "N/A",
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

    private static MyBookingResponse MapToMyResponse(Booking booking)
    {
        return new MyBookingResponse(
            booking.BookingID,
            booking.BookingCode,
            booking.ShowtimeID,
            new BookingMovieResponse(
                booking.Showtime?.Movie.Title ?? "F&B Only",
                booking.Showtime?.Movie.PosterURL,
                booking.Showtime?.Movie.AgeRating ?? "N/A",
                booking.Showtime?.Movie.DurationMin ?? 0),
            booking.Showtime?.StartTime ?? DateTime.MinValue,
            booking.Showtime?.Room.Cinema.CinemaName ?? "N/A",
            booking.Showtime?.Room.RoomName ?? "N/A",
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

    private static CalculatePricingResponse MapToPricingResponse(PricingCalculationResult result)
    {
        return new CalculatePricingResponse
        {
            SeatsSubTotal = result.SeatsSubTotal,
            FnBSubTotal = result.FnBSubTotal,
            TotalBeforeDiscount = result.TotalBeforeDiscount,
            MembershipDiscount = result.MembershipDiscount,
            VoucherDiscount = result.VoucherDiscount,
            TotalDiscount = result.TotalDiscount,
            FinalAmount = result.FinalAmount,
            SeatDetails = result.SeatDetails.Select(s => new SeatPricingResponse
            {
                SeatId = s.SeatId,
                SeatRow = s.SeatRow,
                SeatCol = s.SeatCol,
                SeatTypeName = s.SeatTypeName,
                Price = s.Price
            }).ToList(),
            FnBDetails = result.FnBDetails.Select(f => new FnBPricingResponse
            {
                ItemId = f.ItemId,
                ItemName = f.ItemName,
                Quantity = f.Quantity,
                UnitPrice = f.UnitPrice,
                SubTotal = f.SubTotal
            }).ToList(),
            VoucherDetails = result.VoucherDetails != null
                ? new VoucherPricingResponse
                {
                    VoucherCode = result.VoucherDetails.VoucherCode,
                    DiscountType = result.VoucherDetails.DiscountType,
                    DiscountApplied = result.VoucherDetails.DiscountApplied
                }
                : null
        };
    }
}
