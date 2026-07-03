using CinemaBooking.API.Contracts.Tickets;
using CinemaBooking.Application.Tickets;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet("booking/{bookingId}")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> GetTicketsByBookingId(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var tickets = await _ticketService.GetTicketsByBookingIdAsync(bookingId, cancellationToken);

        if (!tickets.Any())
            return NotFound(new { success = false, message = "No tickets found for this booking." });

        var response = tickets.Select(t => new TicketResponse(
            t.TicketID,
            t.QRCode,
            t.Status,
            t.BookingSeatID,
            t.BookingSeat.Seat.SeatID,
            t.BookingSeat.Seat.SeatRow,
            t.BookingSeat.Seat.SeatCol
        )).ToList();

        return Ok(new { success = true, tickets = response });
    }
}
