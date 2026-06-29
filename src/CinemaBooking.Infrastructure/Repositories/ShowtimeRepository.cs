using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class ShowtimeRepository : IShowtimeRepository
{
    private readonly CinemaBookingDbContext _db;

    public ShowtimeRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<(List<Showtime> Items, int TotalItems)> GetShowtimesAsync(
        int? movieId, int? cinemaId, string? movieName, string? roomName,
        DateOnly? date, string? status,
        int page, int pageSize, string sortBy, bool descending,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Showtimes
            .AsNoTracking()
            .Include(showtime => showtime.Movie)
            .Include(showtime => showtime.Room)
                .ThenInclude(room => room.Cinema)
            .AsQueryable();
        if (movieId.HasValue)
            query = query.Where(s => s.MovieID == movieId.Value);
        if (cinemaId.HasValue)
            query = query.Where(s => s.Room.CinemaID == cinemaId.Value);
        if (!string.IsNullOrWhiteSpace(movieName))
            query = query.Where(s => EF.Functions.Like(s.Movie.Title, $"%{movieName}%"));
        if (!string.IsNullOrWhiteSpace(roomName))
            query = query.Where(s => EF.Functions.Like(s.Room.RoomName, $"%{roomName}%"));
        if (date.HasValue)
        {
            var from = date.Value.ToDateTime(TimeOnly.MinValue);
            var to = from.AddDays(1);
            query = query.Where(s => s.StartTime >= from && s.StartTime < to);
        }
        if (status is not null) query = query.Where(s => s.Status == status);
        var totalItems = await query.CountAsync(cancellationToken);
        query = sortBy switch
        {
            "endtime" => descending ? query.OrderByDescending(s => s.EndTime) : query.OrderBy(s => s.EndTime),
            "baseprice" => descending ? query.OrderByDescending(s => s.BasePrice) : query.OrderBy(s => s.BasePrice),
            "status" => descending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => descending ? query.OrderByDescending(s => s.StartTime) : query.OrderBy(s => s.StartTime)
        };
        return (await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken), totalItems);
    }

    public Task<Movie?> GetMovieAsync(int movieId, CancellationToken cancellationToken = default) =>
        _db.Movie.FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);
    public Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        _db.Rooms.FirstOrDefaultAsync(r => r.RoomID == roomId, cancellationToken);
    public Task<bool> HasConflictAsync(int roomId, DateTime startTime, DateTime endTime,
        int? excludingShowtimeId = null, CancellationToken cancellationToken = default) =>
        _db.Showtimes.AnyAsync(s => s.RoomID == roomId && s.Status != "cancelled"
            && (!excludingShowtimeId.HasValue || s.ShowtimeID != excludingShowtimeId.Value)
            && s.StartTime < endTime && s.EndTime > startTime, cancellationToken);
    public async Task<bool> HasActiveBookingOrHoldAsync(int showtimeId, DateTime now,
        CancellationToken cancellationToken = default)
    {
        var hasBooking = await _db.Bookings.AnyAsync(b => b.ShowtimeID == showtimeId &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Paid ||
             b.Status == BookingStatus.Used || b.Status == BookingStatus.PartiallyRefunded), cancellationToken);
        return hasBooking || await _db.SeatHolds.AnyAsync(h =>
            h.ShowtimeID == showtimeId && h.Status == SeatHoldStatus.Holding && h.ExpiresAt > now,
            cancellationToken);
    }
    public async Task<bool> IsSoldOutAsync(int showtimeId, int capacity, DateTime now,
        CancellationToken cancellationToken = default)
    {
        var booked = await _db.BookingSeats
            .Where(bs => bs.Booking.ShowtimeID == showtimeId &&
                (bs.Booking.Status == BookingStatus.Pending || bs.Booking.Status == BookingStatus.Paid ||
                 bs.Booking.Status == BookingStatus.Used || bs.Booking.Status == BookingStatus.PartiallyRefunded))
            .Select(bs => bs.SeatID).Distinct().CountAsync(cancellationToken);
        var held = await _db.SeatHolds
            .Where(h => h.ShowtimeID == showtimeId && h.Status == SeatHoldStatus.Holding && h.ExpiresAt > now)
            .Select(h => h.SeatID).Distinct().CountAsync(cancellationToken);
        return booked + held >= capacity;
    }
    public async Task<Showtime> AddAsync(Showtime showtime, CancellationToken cancellationToken = default)
    {
        _db.Showtimes.Add(showtime); await _db.SaveChangesAsync(cancellationToken);
        return (await GetShowtimeByIdAsync(showtime.ShowtimeID, cancellationToken))!;
    }
    public async Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
        return await GetShowtimeByIdAsync(showtime.ShowtimeID, cancellationToken);
    }
    public async Task<bool> DeleteAsync(int showtimeId, CancellationToken cancellationToken = default)
    {
        var showtime = await _db.Showtimes.FindAsync([showtimeId], cancellationToken);
        if (showtime is null) return false;
        _db.Showtimes.Remove(showtime); await _db.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<Showtime?> GetShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.ShowtimeID == id
                && s.Room.Cinema.Status == "active", cancellationToken);
    }

    public async Task<List<Seat>> GetSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Seats
            .Include(seat => seat.SeatType)
            .Where(seat => seat.RoomID == roomId && seat.Status == "active")
            .OrderBy(seat => seat.SeatRow)
            .ThenBy(seat => seat.SeatCol)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetBookedSeatIdsAsync(
    int showtimeId,
    CancellationToken cancellationToken = default)
    {
        return await _db.BookingSeats
            .Where(bs => bs.Booking.ShowtimeID == showtimeId
                      && (bs.Booking.Status == BookingStatus.Pending
                          || bs.Booking.Status == BookingStatus.Paid
                          || bs.Booking.Status == BookingStatus.Used))
            .Select(bs => bs.SeatID)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetHeldSeatIdsAsync(
    int showtimeId,
    CancellationToken cancellationToken = default)
    {
        return await _db.SeatHolds
            .Where(h => h.ShowtimeID == showtimeId
                      && h.Status == SeatHoldStatus.Holding
                      && h.ExpiresAt > DateTime.UtcNow)
            .Select(h => h.SeatID)
            .ToListAsync(cancellationToken);
    }
}
