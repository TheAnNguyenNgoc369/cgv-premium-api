using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<Ticket> CreateTicketAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetTicketedBookingSeatIdsAsync(
        int bookingId,
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

    Task<List<Ticket>> GetTicketsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task UpdateTicketsStatusByBookingAsync(
        int bookingId,
        string status,
        CancellationToken cancellationToken = default);

    Task<Ticket?> GetTicketWithFullDetailsForCheckInAsync(
        string qrCode,
        CancellationToken cancellationToken = default);

    Task<(bool Success, bool BookingBecameUsed)> PerformTicketCheckInAsync(
        int ticketId,
        int bookingId,
        int staffId,
        string? ipAddress,
        DateTime checkedInAt,
        CancellationToken cancellationToken = default);

    Task<bool> AreAllTicketsUsedInBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default);
}
