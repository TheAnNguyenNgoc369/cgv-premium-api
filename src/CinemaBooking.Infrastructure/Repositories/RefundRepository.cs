using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class RefundRepository : IRefundRepository
{
    private readonly CinemaBookingDbContext _db;

    public RefundRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Refund> CreateRefundAsync(
        Refund refund,
        CancellationToken cancellationToken = default)
    {
        await _db.Refunds.AddAsync(refund, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return refund;
    }

    public async Task<Refund?> GetRefundByIdAsync(
        int refundId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Refunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Room)
                        .ThenInclude(r => r.Cinema)
            .Include(r => r.Booking)
                .ThenInclude(b => b.User)
            .Include(r => r.Payment)
            .Include(r => r.Wallet)
            .Include(r => r.ProcessedByUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.RefundID == refundId, cancellationToken);
    }

    public async Task<List<Refund>> GetRefundsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Refunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Room)
                        .ThenInclude(r => r.Cinema)
            .Include(r => r.Payment)
            .Include(r => r.ProcessedByUser)
            .AsSplitQuery()
            .Where(r => r.Booking.UserID == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Refund>> GetAllRefundsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Refunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Room)
                        .ThenInclude(r => r.Cinema)
            .Include(r => r.Booking)
                .ThenInclude(b => b.User)
            .Include(r => r.Payment)
            .Include(r => r.ProcessedByUser)
            .AsSplitQuery()
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateRefundStatusAsync(
        int refundId,
        string status,
        DateTime processedAt,
        int processedBy,
        CancellationToken cancellationToken = default)
    {
        var refund = await _db.Refunds.FindAsync(new object[] { refundId }, cancellationToken);
        if (refund is null)
            throw new InvalidOperationException($"Refund {refundId} not found");

        refund.Status = status;
        refund.CompletedAt = processedAt;
        refund.ProcessedBy = processedBy;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountCompletedRefundsInCurrentMonthAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        return await _db.Refunds
            .Where(r => r.Booking.UserID == userId
                     && r.Status == "completed"
                     && r.CompletedAt >= startOfMonth
                     && r.CompletedAt < endOfMonth)
            .CountAsync(cancellationToken);
    }

    public async Task<Booking?> GetBookingForRefundAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Room)
                    .ThenInclude(r => r.Cinema)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(b => b.Payment)
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Ticket)
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Seat)
            .Include(b => b.User)
                .ThenInclude(u => u!.LoyaltyTier)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.BookingID == bookingId, cancellationToken);
    }
}
