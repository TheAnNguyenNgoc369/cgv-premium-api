using CinemaBooking.Application.Common.Interfaces;
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

    public Task<SeatType?> GetSeatTypeByNameAsync(
        string typeName,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SeatTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.TypeName == typeName, cancellationToken);
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

    public Task<bool> HasActiveOrUpcomingShowtimesAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Showtimes
            .AsNoTracking()
            .AnyAsync(showtime => showtime.RoomID == roomId
                && (showtime.Status == "scheduled" || showtime.Status == "ongoing"), cancellationToken);
    }

    public async Task<bool> HasSeatRelationsAsync(
        int seatId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BookingSeats
            .AsNoTracking()
            .AnyAsync(bookingSeat => bookingSeat.SeatID == seatId, cancellationToken)
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

        await _dbContext.Entry(seat)
            .Reference(s => s.SeatType)
            .LoadAsync(cancellationToken);

        return seat;
    }

    public async Task<Seat?> UpdateAsync(
        int roomId,
        int seatId,
        int seatTypeId,
        string status,
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(seat)
            .Reference(s => s.SeatType)
            .LoadAsync(cancellationToken);

        return seat;
    }

    public async Task<bool> DeleteAsync(
        int roomId,
        int seatId,
        CancellationToken cancellationToken = default)
    {
        var seat = await _dbContext.Seats
            .FirstOrDefaultAsync(
                s => s.RoomID == roomId && s.SeatID == seatId,
                cancellationToken);

        if (seat is null)
        {
            return false;
        }

        _dbContext.Seats.Remove(seat);
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
}
