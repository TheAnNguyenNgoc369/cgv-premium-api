using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
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

    public async Task<Ticket?> GetTicketWithFullDetailsForCheckInAsync(
        string qrCode,
        CancellationToken cancellationToken = default)
    {
        return await _db.Tickets
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Seat)
                    .ThenInclude(s => s.SeatType)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.User)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.Showtime)
                        .ThenInclude(s => s.Movie)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.Showtime)
                        .ThenInclude(s => s.Room)
                            .ThenInclude(r => r.Cinema)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.Showtime)
                        .ThenInclude(s => s.Room)
                            .ThenInclude(r => r.RoomType)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.Payment)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.Refunds)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.BookingSeats)
                        .ThenInclude(bs => bs.Ticket)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.BookingSeats)
                        .ThenInclude(bs => bs.Seat)
                            .ThenInclude(s => s.SeatType)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.BookingFnBs)
                        .ThenInclude(fnb => fnb.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.QRCode == qrCode, cancellationToken);
    }

    public async Task<bool> PerformTicketCheckInAsync(
        int ticketId,
        int bookingId,
        int staffId,
        string? ipAddress,
        DateTime checkedInAt,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var ticket = await _db.Tickets
                    .FirstOrDefaultAsync(t => t.TicketID == ticketId, cancellationToken);

                if (ticket is null)
                    return false;

                ticket.Status = TicketStatus.Used;
                ticket.CheckedInAt = checkedInAt;
                ticket.CheckedInByID = staffId;

                var allTicketsUsed = await AreAllTicketsUsedInBookingAsync(bookingId, cancellationToken);

                if (allTicketsUsed)
                {
                    var booking = await _db.Bookings
                        .FirstOrDefaultAsync(b => b.BookingID == bookingId, cancellationToken);

                    if (booking is not null)
                    {
                        booking.Status = BookingStatus.Used;
                        booking.UpdatedAt = checkedInAt;
                    }
                }

                var auditLog = new Domain.Entities.AdminActionLog
                {
                    AdminID = staffId,
                    TargetTable = "Ticket",
                    TargetID = ticket.TicketID,
                    ActionType = AdminActionTypes.CheckIn,
                    Description = $"Staff checked in ticket {ticket.QRCode} for booking {bookingId}",
                    IPAddress = ipAddress ?? "unknown",
                    CreatedAt = checkedInAt
                };

                await _db.AdminActionLogs.AddAsync(auditLog, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                Console.WriteLine($"Check-in failed: {ex.Message}");
                throw;
            }
        });
    }

    public async Task<bool> AreAllTicketsUsedInBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var tickets = await _db.Tickets
            .Where(t => t.BookingSeat.BookingID == bookingId)
            .ToListAsync(cancellationToken);

        return tickets.Any() && tickets.All(t => t.Status == "used");
    }
}
