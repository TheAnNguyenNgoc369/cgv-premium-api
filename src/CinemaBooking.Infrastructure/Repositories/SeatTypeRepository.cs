using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class SeatTypeRepository : ISeatTypeRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public SeatTypeRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<SeatType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SeatTypes
            .AsNoTracking()
            .OrderBy(seatType => seatType.TypeName)
            .ToListAsync(cancellationToken);
    }

    public Task<SeatType?> GetByIdAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SeatTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                seatType => seatType.SeatTypeID == seatTypeId,
                cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string typeName,
        int? excludingSeatTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SeatTypes
            .AsNoTracking()
            .Where(seatType => seatType.TypeName == typeName);

        if (excludingSeatTypeId.HasValue)
        {
            query = query.Where(seatType => seatType.SeatTypeID != excludingSeatTypeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<SeatType> AddAsync(
        SeatType seatType,
        CancellationToken cancellationToken = default)
    {
        _dbContext.SeatTypes.Add(seatType);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return seatType;
    }

    public async Task<SeatType?> UpdateAsync(
        int seatTypeId,
        string typeName,
        decimal extraPrice,
        CancellationToken cancellationToken = default)
    {
        var seatType = await _dbContext.SeatTypes
            .FirstOrDefaultAsync(
                item => item.SeatTypeID == seatTypeId,
                cancellationToken);

        if (seatType is null)
        {
            return null;
        }

        seatType.TypeName = typeName;
        seatType.ExtraPrice = extraPrice;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return seatType;
    }

    public Task<bool> IsUsedByAnySeatAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Seats
            .AsNoTracking()
            .AnyAsync(seat => seat.SeatTypeID == seatTypeId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default)
    {
        var seatType = await _dbContext.SeatTypes
            .FirstOrDefaultAsync(
                item => item.SeatTypeID == seatTypeId,
                cancellationToken);

        if (seatType is null)
        {
            return false;
        }

        _dbContext.SeatTypes.Remove(seatType);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
