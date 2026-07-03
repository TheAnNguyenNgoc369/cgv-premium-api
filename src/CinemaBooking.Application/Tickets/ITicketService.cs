using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Tickets;

public interface ITicketService
{
    Task CreateTicketsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<List<Ticket>?> GetTicketsByBookingIdAsync(
        int bookingId,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default);

    Task<List<Ticket>> GetMyTicketsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
