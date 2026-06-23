using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
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
                      && (bs.Booking.Status == "pending"
                          || bs.Booking.Status == "paid"
                          || bs.Booking.Status == "used"))
            .Select(bs => bs.SeatID)
            .ToListAsync(cancellationToken);

        var heldByOthersSeatIds = await _db.SeatHolds
            .Where(h => h.ShowtimeID == showtimeId
                      && seatIds.Contains(h.SeatID)
                      && h.Status == "holding"
                      && h.ExpiresAt > DateTime.Now
                      && h.UserID != currentUserId)
            .Select(h => h.SeatID)
            .ToListAsync(cancellationToken);

        return bookedSeatIds.Union(heldByOthersSeatIds).Distinct().ToList();
    }

    public async Task AddSeatHoldsAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default)
    {
        await _db.SeatHolds.AddRangeAsync(seatHolds, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
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
                      && h.Status == "holding"
                      && h.ExpiresAt > DateTime.Now)
            .ToListAsync(cancellationToken);
    }

    public async Task AddBookingAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        await _db.Bookings.AddAsync(booking, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkHoldsAsConfirmedAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default)
    {
        foreach (var hold in seatHolds)
        {
            hold.Status = "confirmed";
        }

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
            .Include(b => b.BookingFnBs).ThenInclude(fnb => fnb.Product)
            .Include(b => b.BookingVoucher!).ThenInclude(bv => bv.Voucher)
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
            .Include(b => b.BookingFnBs).ThenInclude(fnb => fnb.Product)
            .Include(b => b.BookingVoucher!).ThenInclude(bv => bv.Voucher)
            .Where(b => b.UserID == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetProductsByIdsAsync(
        List<int> productIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .Where(p => productIds.Contains(p.ItemID))
            .ToListAsync(cancellationToken);
    }

    public async Task<Voucher?> GetVoucherByCodeAsync(
        string voucherCode,
        CancellationToken cancellationToken = default)
    {
        return await _db.Vouchers
            .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode, cancellationToken);
    }

    public async Task IncrementVoucherUsageAsync(
        int voucherId,
        CancellationToken cancellationToken = default)
    {
        var voucher = await _db.Vouchers.FindAsync(new object[] { voucherId }, cancellationToken);
        if (voucher is not null)
        {
            voucher.UsedCount++;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}