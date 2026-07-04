using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Seats;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class SeatRepository : ISeatRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public SeatRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Seat>> GetSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Seats
            .AsNoTracking()
            .Include(seat => seat.SeatType)
            .Where(seat => seat.RoomID == roomId)
            .OrderBy(seat => seat.SeatRow)
            .ThenBy(seat => seat.SeatCol)
            .ToListAsync(cancellationToken);
    }

    public Task<Room?> GetRoomByIdAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(room => room.RoomID == roomId, cancellationToken);
    }

    public Task<Seat?> GetSeatByIdAsync(
        int roomId,
        int seatId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Seats
            .AsNoTracking()
            .Include(seat => seat.SeatType)
            .FirstOrDefaultAsync(
                seat => seat.RoomID == roomId && seat.SeatID == seatId,
                cancellationToken);
    }

    public Task<Seat?> GetSeatByIdAsync(
        int seatId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Seats
            .AsNoTracking()
            .Include(seat => seat.SeatType)
            .FirstOrDefaultAsync(seat => seat.SeatID == seatId, cancellationToken);
    }

    public Task<SeatType?> GetSeatTypeByNameAsync(
        string typeName,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SeatTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.TypeName == typeName, cancellationToken);
    }

    public Task<SeatType?> GetSeatTypeByIdAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SeatTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.SeatTypeID == seatTypeId, cancellationToken);
    }

    public Task<int> CountSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Seats
            .AsNoTracking()
            .CountAsync(seat => seat.RoomID == roomId, cancellationToken);
    }

    public Task<bool> SeatPositionExistsAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        int? excludingSeatId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Seats
            .AsNoTracking()
            .Where(seat => seat.RoomID == roomId
                && seat.SeatRow == rowLabel
                && seat.SeatCol == seatNumber);

        if (excludingSeatId.HasValue)
        {
            query = query.Where(seat => seat.SeatID != excludingSeatId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<List<Seat>> GetSeatsBySelectorAsync(
        int roomId,
        SeatSelector selector,
        CancellationToken cancellationToken = default)
    {
        var normalizedMode = selector.Mode?.Trim().ToUpperInvariant();
        if (normalizedMode is null)
        {
            return Task.FromResult(new List<Seat>());
        }

        IQueryable<Seat> query = _dbContext.Seats
            .AsNoTracking()
            .Include(seat => seat.SeatType)
            .Where(seat => seat.RoomID == roomId);

        if (normalizedMode == "IDS")
        {
            var ids = selector.Target
                .Select(value => int.TryParse(value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToArray();

            query = query.Where(seat => ids.Contains(seat.SeatID));
        }
        else if (normalizedMode == "ROWS")
        {
            var rows = selector.Target
                .Select(value => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant())
                .Where(value => value is not null)
                .ToArray();

            query = query.Where(seat => rows.Contains(seat.SeatRow));
        }
        else if (normalizedMode == "COLS")
        {
            var cols = selector.Target
                .Select(value => int.TryParse(value, out var col) ? col : (int?)null)
                .Where(col => col.HasValue)
                .Select(col => col!.Value)
                .ToArray();

            query = query.Where(seat => cols.Contains(seat.SeatCol));
        }
        else
        {
            return Task.FromResult(new List<Seat>());
        }

        return query
            .OrderBy(seat => seat.SeatRow)
            .ThenBy(seat => seat.SeatCol)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveOrUpcomingShowtimesAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Showtimes
            .AsNoTracking()
            .AnyAsync(showtime => showtime.RoomID == roomId
                && showtime.Status == "scheduled", cancellationToken);
    }

    public async Task<bool> HasSeatRelationsAsync(
        int seatId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BookingSeats
            .AsNoTracking()
            .AnyAsync(bookingSeat => bookingSeat.SeatID == seatId, cancellationToken)
            || await _dbContext.Tickets
                .AsNoTracking()
                .AnyAsync(ticket => ticket.BookingSeat.SeatID == seatId, cancellationToken)
            || await _dbContext.SeatHolds
                .AsNoTracking()
                .AnyAsync(hold => hold.SeatID == seatId, cancellationToken);
    }

    public async Task<Seat> AddAsync(
        Seat seat,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Seats.Add(seat);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateRoomCapacityAsync(seat.RoomID, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(seat)
            .Reference(s => s.SeatType)
            .LoadAsync(cancellationToken);

        return seat;
    }

    public async Task<Seat?> UpdateAsync(
        int roomId,
        int seatId,
        int? seatTypeId,
        string status,
        bool isGap,
        CancellationToken cancellationToken = default)
    {
        var seat = await _dbContext.Seats
            .Include(s => s.SeatType)
            .FirstOrDefaultAsync(
                s => s.RoomID == roomId && s.SeatID == seatId,
                cancellationToken);

        if (seat is null)
        {
            return null;
        }

        seat.SeatTypeID = seatTypeId;
        seat.Status = status;
        seat.IsGap = isGap;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateRoomCapacityAsync(roomId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(seat)
            .Reference(s => s.SeatType)
            .LoadAsync(cancellationToken);

        return seat;
    }

    public async Task<bool> DeleteAsync(
        int seatId,
        CancellationToken cancellationToken = default)
    {
        var seat = await _dbContext.Seats
            .FirstOrDefaultAsync(s => s.SeatID == seatId, cancellationToken);

        if (seat is null)
        {
            return false;
        }

        var roomId = seat.RoomID;
        _dbContext.Seats.Remove(seat);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateRoomCapacityAsync(roomId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<List<Seat>> ReplaceLayoutAsync(
        int roomId,
        IReadOnlyCollection<Seat> seats,
        CancellationToken cancellationToken = default)
    {
        var existingSeats = await _dbContext.Seats
            .Where(seat => seat.RoomID == roomId)
            .ToListAsync(cancellationToken);

        var relatedSeatIds = await GetRelatedSeatIdsAsync(roomId, cancellationToken);
        var requestedSeats = seats.ToDictionary(
            seat => (seat.SeatRow, seat.SeatCol),
            seat => seat);

        foreach (var existingSeat in existingSeats)
        {
            if (requestedSeats.Remove((existingSeat.SeatRow, existingSeat.SeatCol), out var requestedSeat))
            {
                existingSeat.SeatTypeID = requestedSeat.SeatTypeID;
                existingSeat.Status = requestedSeat.Status;
                continue;
            }

            if (relatedSeatIds.Contains(existingSeat.SeatID))
            {
                existingSeat.Status = "inactive";
                continue;
            }

            _dbContext.Seats.Remove(existingSeat);
        }

        _dbContext.Seats.AddRange(requestedSeats.Values);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateRoomCapacityAsync(roomId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetSeatsByRoomAsync(roomId, cancellationToken);
    }

    private async Task<HashSet<int>> GetRelatedSeatIdsAsync(
        int roomId,
        CancellationToken cancellationToken)
    {
        var bookingSeatIds = await _dbContext.BookingSeats
            .AsNoTracking()
            .Where(bookingSeat => bookingSeat.Seat.RoomID == roomId)
            .Select(bookingSeat => bookingSeat.SeatID)
            .ToListAsync(cancellationToken);

        var holdSeatIds = await _dbContext.SeatHolds
            .AsNoTracking()
            .Where(hold => hold.Seat.RoomID == roomId)
            .Select(hold => hold.SeatID)
            .ToListAsync(cancellationToken);

        return bookingSeatIds
            .Concat(holdSeatIds)
            .ToHashSet();
    }

    private async Task RecalculateRoomCapacityAsync(
        int roomId,
        CancellationToken cancellationToken)
    {
        var capacity = await _dbContext.Seats
            .Where(seat => seat.RoomID == roomId)
            .Join(
                _dbContext.SeatTypes,
                seat => seat.SeatTypeID,
                seatType => seatType.SeatTypeID,
                (_, seatType) => seatType.Capacity)
            .SumAsync(cancellationToken);

        var room = await _dbContext.Rooms
            .FirstOrDefaultAsync(item => item.RoomID == roomId, cancellationToken);

        if (room is not null)
        {
            room.Capacity = capacity;
        }
    }
}
