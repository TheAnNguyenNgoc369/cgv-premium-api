using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class RoomTypeRepository : IRoomTypeRepository
{
    private readonly CinemaBookingDbContext _db;

    public RoomTypeRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<int?> GetRoomTypeIdByNameAsync(
        string typeName,
        CancellationToken cancellationToken = default)
    {
        var roomType = await _db.Set<Domain.Entities.RoomType>()
            .Where(rt => rt.TypeName == typeName)
            .Select(rt => rt.RoomTypeID)
            .FirstOrDefaultAsync(cancellationToken);

        return roomType == 0 ? null : roomType;
    }
}
