using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<Ticket> CreateTicketAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);

    Task<List<Ticket>> GetTicketsByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Ticket?> GetTicketByIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    Task<Ticket?> GetTicketByQRCodeAsync(
        string qrCode,
        CancellationToken cancellationToken = default);
}
