using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly CinemaBookingDbContext _db;

    public BookingRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Showtime?> GetShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Showtimes
            .FirstOrDefaultAsync(s => s.ShowtimeID == showtimeId, cancellationToken);
    }

    public async Task<List<Seat>> GetSeatsByIdsAsync(
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.Seats
            .Include(s => s.SeatType)
            .Where(s => seatIds.Contains(s.SeatID))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetUnavailableSeatIdsAsync(
        int showtimeId,
        List<int> seatIds,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var bookedSeatIds = await _db.BookingSeats
            .Where(bs => bs.Booking.ShowtimeID == showtimeId
                      && seatIds.Contains(bs.SeatID)
                      && (bs.Booking.Status == BookingStatus.Pending
                          || bs.Booking.Status == BookingStatus.Paid
                          || bs.Booking.Status == BookingStatus.Used))
            .Select(bs => bs.SeatID)
            .ToListAsync(cancellationToken);

        var heldByOthersSeatIds = await _db.SeatHolds
            .Where(h => h.ShowtimeID == showtimeId
                      && seatIds.Contains(h.SeatID)
                      && h.Status == SeatHoldStatus.Holding
                      && h.ExpiresAt > DateTime.UtcNow
                      && h.UserID != currentUserId)
            .Select(h => h.SeatID)
            .ToListAsync(cancellationToken);

        return bookedSeatIds.Union(heldByOthersSeatIds).Distinct().ToList();
    }

    public async Task ExpireStaleHoldsAsync(
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        var staleHolds = await _db.SeatHolds
            .Where(h => h.ShowtimeID == showtimeId
                      && seatIds.Contains(h.SeatID)
                      && h.Status == SeatHoldStatus.Holding
                      && h.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (staleHolds.Count == 0)
            return;

        foreach (var hold in staleHolds)
            hold.Status = SeatHoldStatus.Expired;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAddSeatHoldsAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default)
    {
        var holdsList = seatHolds.ToList();
        await _db.SeatHolds.AddRangeAsync(holdsList, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Gỡ entity vừa add ra khỏi change tracker, tránh lỗi ở SaveChanges lần sau
            foreach (var hold in holdsList)
                _db.Entry(hold).State = EntityState.Detached;

            return false;
        }
    }

    public async Task<List<SeatHold>> GetMyActiveHoldsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.SeatHolds
            .Where(h => h.UserID == userId
                      && h.ShowtimeID == showtimeId
                      && seatIds.Contains(h.SeatID)
                      && h.Status == SeatHoldStatus.Holding
                      && h.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateBookingAndConfirmHoldsAsync(
        Booking booking,
        IEnumerable<SeatHold> holdsToConfirm,
        CancellationToken cancellationToken = default)
    {
        await _db.Bookings.AddAsync(booking, cancellationToken);

        foreach (var hold in holdsToConfirm)
            hold.Status = SeatHoldStatus.Confirmed;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .FirstOrDefaultAsync(b => b.BookingID == bookingId, cancellationToken);
    }

    public async Task<List<Booking>> GetBookingsByUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Where(b => b.UserID == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync(cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // 2627 = vi phạm UNIQUE constraint, 2601 = vi phạm UNIQUE index
        return ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2627 || sqlEx.Number == 2601);
    }
}