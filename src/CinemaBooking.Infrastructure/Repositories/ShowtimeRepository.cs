using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class ShowtimeRepository : IShowtimeRepository
{
    private readonly CinemaBookingDbContext _db;

    public ShowtimeRepository(CinemaBookingDbContext db) => _db = db;

    public async Task<(List<Showtime> Items, int TotalItems)> GetShowtimesAsync(
        int? movieId, int? cinemaId, string? movieName, string? roomName,
        DateOnly? date, string? status, bool onlyActiveLocations,
        int page, int pageSize, string sortBy, bool descending,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Showtimes.AsNoTracking()
            .Include(showtime => showtime.Movie)
            .Include(showtime => showtime.Room).ThenInclude(room => room.Cinema)
            .AsQueryable();
        if (onlyActiveLocations)
            query = query.Where(s => s.Room.Status == "active" && s.Room.Cinema.Status == "active");
        if (movieId.HasValue) query = query.Where(s => s.MovieID == movieId.Value);
        if (cinemaId.HasValue) query = query.Where(s => s.Room.CinemaID == cinemaId.Value);
        if (!string.IsNullOrWhiteSpace(movieName))
            query = query.Where(s => EF.Functions.Like(s.Movie.Title, $"%{movieName}%"));
        if (!string.IsNullOrWhiteSpace(roomName))
            query = query.Where(s => EF.Functions.Like(s.Room.RoomName, $"%{roomName}%"));
        if (date.HasValue)
        {
            var (from, to) = VietnamTime.GetUtcDayRange(date.Value);
            query = query.Where(s => s.StartTime >= from && s.StartTime < to);
        }
        if (status is not null) query = query.Where(s => s.Status == status);

        var totalItems = await query.CountAsync(cancellationToken);
        IOrderedQueryable<Showtime> ordered = sortBy switch
        {
            "endtime" => descending ? query.OrderByDescending(s => s.EndTime) : query.OrderBy(s => s.EndTime),
            "baseprice" => descending ? query.OrderByDescending(s => s.BasePrice) : query.OrderBy(s => s.BasePrice),
            "status" => descending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => descending ? query.OrderByDescending(s => s.StartTime) : query.OrderBy(s => s.StartTime)
        };
        ordered = descending
            ? ordered.ThenByDescending(s => s.ShowtimeID)
            : ordered.ThenBy(s => s.ShowtimeID);
        var skip = (long)(page - 1) * pageSize;
        if (skip > int.MaxValue) return ([], totalItems);
        return (await ordered.Skip((int)skip).Take(pageSize).ToListAsync(cancellationToken), totalItems);
    }

    public Task<Movie?> GetMovieAsync(int movieId, CancellationToken cancellationToken = default) =>
        _db.Movie.FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);

    public Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        _db.Rooms.Include(room => room.Cinema)
            .FirstOrDefaultAsync(r => r.RoomID == roomId, cancellationToken);

    public Task<bool> HasConflictAsync(
        int roomId, DateTime startTime, DateTime endTime, int? excludingShowtimeId = null,
        CancellationToken cancellationToken = default) =>
        _db.Showtimes.AnyAsync(s => s.RoomID == roomId && s.Status != "cancelled"
            && (!excludingShowtimeId.HasValue || s.ShowtimeID != excludingShowtimeId.Value)
            && s.StartTime < endTime && s.EndTime > startTime, cancellationToken);

    public Task<bool> HasRoomTypeStartConflictAsync(
        int cinemaId, string roomType, DateTime startTime, int? excludingShowtimeId = null,
        CancellationToken cancellationToken = default) =>
        _db.Showtimes.AnyAsync(s => s.Room.CinemaID == cinemaId
            && s.Room.RoomType == roomType
            && s.StartTime == startTime
            && s.Status != "cancelled"
            && (!excludingShowtimeId.HasValue || s.ShowtimeID != excludingShowtimeId.Value),
            cancellationToken);

    public async Task<bool> HasActiveBookingOrHoldAsync(
        int showtimeId, DateTime now, CancellationToken cancellationToken = default)
    {
        var hasBooking = await _db.Bookings.AnyAsync(b => b.ShowtimeID == showtimeId
            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Paid
                || b.Status == BookingStatus.Used || b.Status == BookingStatus.PartiallyRefunded),
            cancellationToken);
        return hasBooking || await _db.SeatHolds.AnyAsync(h => h.ShowtimeID == showtimeId
            && (h.Status == SeatHoldStatus.Holding || h.Status == SeatHoldStatus.Confirmed)
            && h.ExpiresAt > now, cancellationToken);
    }

    public async Task<bool> HasAnyBookingOrHoldAsync(
        int showtimeId, CancellationToken cancellationToken = default)
    {
        if (await _db.Bookings.AnyAsync(
                booking => booking.ShowtimeID == showtimeId, cancellationToken))
            return true;
        return await _db.SeatHolds.AnyAsync(
            hold => hold.ShowtimeID == showtimeId, cancellationToken);
    }

    public async Task<IReadOnlySet<int>> GetSoldOutShowtimeIdsAsync(
        IReadOnlyCollection<int> showtimeIds, DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (showtimeIds.Count == 0) return new HashSet<int>();
        var ids = showtimeIds.Distinct().ToArray();
        var showtimeRooms = await _db.Showtimes.AsNoTracking()
            .Where(s => ids.Contains(s.ShowtimeID))
            .Select(s => new { s.ShowtimeID, s.RoomID })
            .ToListAsync(cancellationToken);
        var roomIds = showtimeRooms.Select(item => item.RoomID).Distinct().ToArray();
        var activeSeats = await _db.Seats.AsNoTracking()
            .Where(seat => roomIds.Contains(seat.RoomID) && seat.IsCurrentLayout && seat.Status == "active"
                && !seat.IsGap && seat.SeatTypeID.HasValue)
            .Select(seat => new { seat.RoomID, seat.SeatID, seat.SeatType!.Capacity })
            .ToListAsync(cancellationToken);
        var capacityByRoom = activeSeats.GroupBy(seat => seat.RoomID)
            .ToDictionary(group => group.Key, group => group.Sum(seat => seat.Capacity));
        var seatCapacity = activeSeats.ToDictionary(seat => seat.SeatID, seat => seat.Capacity);

        var booked = await _db.BookingSeats.AsNoTracking()
            .Where(bs => ids.Contains(bs.Booking.ShowtimeID)
                && (bs.Booking.Status == BookingStatus.Pending || bs.Booking.Status == BookingStatus.Paid
                    || bs.Booking.Status == BookingStatus.Used
                    || bs.Booking.Status == BookingStatus.PartiallyRefunded))
            .Select(bs => new { bs.Booking.ShowtimeID, bs.SeatID })
            .ToListAsync(cancellationToken);
        var held = await _db.SeatHolds.AsNoTracking()
            .Where(h => ids.Contains(h.ShowtimeID)
                && (h.Status == SeatHoldStatus.Holding || h.Status == SeatHoldStatus.Confirmed)
                && h.ExpiresAt > now)
            .Select(h => new { h.ShowtimeID, h.SeatID })
            .ToListAsync(cancellationToken);
        var unavailableCapacity = booked.Select(item => (item.ShowtimeID, item.SeatID))
            .Concat(held.Select(item => (item.ShowtimeID, item.SeatID)))
            .Distinct()
            .Where(item => seatCapacity.ContainsKey(item.SeatID))
            .GroupBy(item => item.ShowtimeID)
            .ToDictionary(group => group.Key, group => group.Sum(item => seatCapacity[item.SeatID]));

        return showtimeRooms
            .Where(item => capacityByRoom.GetValueOrDefault(item.RoomID) > 0
                && unavailableCapacity.GetValueOrDefault(item.ShowtimeID)
                    >= capacityByRoom.GetValueOrDefault(item.RoomID))
            .Select(item => item.ShowtimeID)
            .ToHashSet();
    }

    public async Task<bool> AcquireRoomScheduleLockAsync(
        int roomId, CancellationToken cancellationToken = default)
    {
        var room = await _db.Rooms
            .FromSqlInterpolated($"SELECT * FROM [Room] WITH (UPDLOCK, HOLDLOCK) WHERE RoomID = {roomId}")
            .FirstOrDefaultAsync(cancellationToken);
        return room is not null;
    }

    public async Task<bool> AcquireCinemaScheduleLockAsync(
        int cinemaId, CancellationToken cancellationToken = default)
    {
        var cinema = await _db.Cinemas
            .FromSqlInterpolated($"SELECT * FROM [Cinema] WITH (UPDLOCK, HOLDLOCK) WHERE CinemaID = {cinemaId}")
            .FirstOrDefaultAsync(cancellationToken);
        return cinema is not null;
    }

    public async Task<Showtime> AddAsync(Showtime showtime, CancellationToken cancellationToken = default)
    {
        _db.Showtimes.Add(showtime);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetManagedShowtimeByIdAsync(showtime.ShowtimeID, cancellationToken))!;
    }

    public async Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
        return await GetManagedShowtimeByIdAsync(showtime.ShowtimeID, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int showtimeId, CancellationToken cancellationToken = default)
    {
        var showtime = await _db.Showtimes.FindAsync([showtimeId], cancellationToken);
        if (showtime is null) return false;
        _db.Showtimes.Remove(showtime);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<Showtime?> GetShowtimeByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Showtimes.AsNoTracking()
            .Include(s => s.Movie).Include(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.ShowtimeID == id && s.Room.Status == "active"
                && s.Room.Cinema.Status == "active", cancellationToken);

    public Task<Showtime?> GetManagedShowtimeByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        _db.Showtimes.Include(s => s.Movie).Include(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.ShowtimeID == id, cancellationToken);

    public Task<List<Seat>> GetSeatsByRoomAsync(
        int roomId, CancellationToken cancellationToken = default) =>
        _db.Seats.Include(seat => seat.SeatType)
            .Where(seat => seat.RoomID == roomId && seat.IsCurrentLayout
                && (seat.IsGap || (seat.Status == "active" && seat.SeatTypeID.HasValue)))
            .OrderBy(seat => seat.SeatRow).ThenBy(seat => seat.SeatCol)
            .ToListAsync(cancellationToken);

    public Task<List<int>> GetBookedSeatIdsAsync(
        int showtimeId, CancellationToken cancellationToken = default) =>
        _db.BookingSeats.Where(bs => bs.Booking.ShowtimeID == showtimeId
            && (bs.Booking.Status == BookingStatus.Pending || bs.Booking.Status == BookingStatus.Paid
                || bs.Booking.Status == BookingStatus.Used
                || bs.Booking.Status == BookingStatus.PartiallyRefunded))
            .Select(bs => bs.SeatID).Distinct().ToListAsync(cancellationToken);

    public Task<List<int>> GetHeldSeatIdsAsync(
        int showtimeId, DateTime now, CancellationToken cancellationToken = default) =>
        _db.SeatHolds.Where(h => h.ShowtimeID == showtimeId
            && (h.Status == SeatHoldStatus.Holding || h.Status == SeatHoldStatus.Confirmed)
            && h.ExpiresAt > now)
            .Select(h => h.SeatID).Distinct().ToListAsync(cancellationToken);
}
