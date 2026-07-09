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
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var isStaff = User.IsInRole(Roles.Staff);

        var tickets = await _ticketService.GetTicketsByBookingIdAsync(
            bookingId, userId, isStaff, cancellationToken);

        if (tickets is null)
            return NotFound(new { success = false, message = "Booking not found or you don't have permission to view these tickets." });

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

    [HttpGet("my")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> GetMyTickets(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var tickets = await _ticketService.GetMyTicketsAsync(userId, cancellationToken);

        return Ok(new { success = true, tickets, count = tickets.Count });
    }
}
