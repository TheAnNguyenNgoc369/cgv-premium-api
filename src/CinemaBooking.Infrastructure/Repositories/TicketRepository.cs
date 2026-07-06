using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly CinemaBookingDbContext _db;

    public TicketRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Ticket> CreateTicketAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        await _db.Tickets.AddAsync(ticket, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<List<Ticket>> GetTicketsByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Tickets
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Seat)
            .Where(t => t.BookingSeat.BookingID == bookingId)
            .OrderBy(t => t.BookingSeat.Seat.SeatRow)
                .ThenBy(t => t.BookingSeat.Seat.SeatCol)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> GetTicketByIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Tickets
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Seat)
            .FirstOrDefaultAsync(t => t.TicketID == ticketId, cancellationToken);
    }

    public async Task<Ticket?> GetTicketByQRCodeAsync(
        string qrCode,
        CancellationToken cancellationToken = default)
    {
        return await _db.Tickets
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Seat)
            .FirstOrDefaultAsync(t => t.QRCode == qrCode, cancellationToken);
    }

    public async Task<List<Ticket>> GetTicketsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Tickets
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Seat)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
            .Where(t => t.BookingSeat.Booking.UserID == userId)
            .OrderByDescending(t => t.BookingSeat.Booking.BookingDate)
                .ThenBy(t => t.BookingSeat.Seat.SeatRow)
                .ThenBy(t => t.BookingSeat.Seat.SeatCol)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateTicketsStatusByBookingAsync(
        int bookingId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var tickets = await _db.Tickets
            .Where(t => t.BookingSeat.BookingID == bookingId)
            .ToListAsync(cancellationToken);

        foreach (var ticket in tickets)
        {
            ticket.Status = status;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
