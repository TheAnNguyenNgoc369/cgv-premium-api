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

    public async Task<List<Showtime>> GetShowtimesByMovieAsync(
        int movieId,
        DateOnly? date,
        int? cinemaId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Showtimes
            .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
            .Where(s => s.MovieID == movieId && s.Status == "scheduled");

        if (date.HasValue)
        {
            var from = date.Value.ToDateTime(TimeOnly.MinValue);
            var to = from.AddDays(1);
            query = query.Where(s => s.StartTime >= from && s.StartTime < to);
        }

        if (cinemaId.HasValue)
            query = query.Where(s => s.Room.CinemaID == cinemaId.Value);

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Showtime?> GetShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.ShowtimeID == id, cancellationToken);
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
