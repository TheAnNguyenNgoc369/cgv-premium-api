using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Tickets;

public sealed class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IBookingRepository _bookingRepository;

    public TicketService(
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository)
    {
        _ticketRepository = ticketRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task CreateTicketsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking is null)
            return;

        foreach (var bookingSeat in booking.BookingSeats)
        {
            var ticket = new Ticket
            {
                BookingSeatID = bookingSeat.BookingSeatID,
                QRCode = Guid.NewGuid().ToString(),
                Status = "valid"
            };

            await _ticketRepository.CreateTicketAsync(ticket, cancellationToken);
        }
    }

    public async Task<List<Ticket>?> GetTicketsByBookingIdAsync(
        int bookingId,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking is null)
            return null;

        if (!isStaff && booking.UserID != actorUserId)
            return null;

        return await _ticketRepository.GetTicketsByBookingIdAsync(bookingId, cancellationToken);
    }

    public async Task<List<Ticket>> GetMyTicketsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _ticketRepository.GetTicketsByUserIdAsync(userId, cancellationToken);
    }
}
