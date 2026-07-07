namespace CinemaBooking.Application.Common.Interfaces;

public interface IRoomTypeRepository
{
    Task<int?> GetRoomTypeIdByNameAsync(string typeName, CancellationToken cancellationToken = default);
}
