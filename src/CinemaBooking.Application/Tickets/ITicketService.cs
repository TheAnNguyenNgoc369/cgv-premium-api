using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Tickets;

public interface ITicketService
{
    Task CreateTicketsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<List<Ticket>> GetTicketsByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);
}
