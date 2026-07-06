using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.RoomTypes;

public interface IRoomTypeService
{
    Task<List<RoomType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoomType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? ErrorMessage, RoomType? RoomType)> CreateAsync(string name, decimal extraPrice, string? description, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? ErrorMessage, RoomType? RoomType)> UpdateAsync(int id, string name, decimal extraPrice, string? description, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? ErrorMessage)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
