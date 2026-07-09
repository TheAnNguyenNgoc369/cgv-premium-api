using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.RoomTypes;

public sealed class RoomTypeService(IRoomTypeRepository repository) : IRoomTypeService
{
    public Task<List<RoomType>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<RoomType?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => repository.GetByIdAsync(id, cancellationToken);
    public Task<(bool Succeeded, string? ErrorMessage, RoomType? RoomType)> CreateAsync(string name, decimal extraPrice, string? description, CancellationToken cancellationToken = default) => SaveAsync(null, name, extraPrice, description, cancellationToken);
    public Task<(bool Succeeded, string? ErrorMessage, RoomType? RoomType)> UpdateAsync(int id, string name, decimal extraPrice, string? description, CancellationToken cancellationToken = default) => SaveAsync(id, name, extraPrice, description, cancellationToken);

    private async Task<(bool Succeeded, string? ErrorMessage, RoomType? RoomType)> SaveAsync(int? id, string name, decimal extraPrice, string? description, CancellationToken cancellationToken)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0) return (false, "Room type name is required.", null);
        if (name.Length > 50) return (false, "Room type name must not exceed 50 characters.", null);
        if (extraPrice < 0) return (false, "Extra price must be greater than or equal to 0.", null);
        if (id.HasValue && await repository.GetByIdAsync(id.Value, cancellationToken) is null) return (false, "Room type not found.", null);
        if (await repository.NameExistsAsync(name, id, cancellationToken)) return (false, "Room type name must be unique.", null);
        if (id.HasValue) return (true, null, await repository.UpdateAsync(id.Value, name, extraPrice, description?.Trim(), cancellationToken));
        var now = DateTime.UtcNow;
        return (true, null, await repository.AddAsync(new RoomType { TypeName = name, ExtraPrice = extraPrice, Description = description?.Trim(), CreatedAt = now, UpdatedAt = now }, cancellationToken));
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await repository.GetByIdAsync(id, cancellationToken) is null) return (false, "Room type not found.");
        if (await repository.IsUsedAsync(id, cancellationToken)) return (false, "Room type is currently used by one or more rooms.");
        return await repository.DeleteAsync(id, cancellationToken) ? (true, null) : (false, "Room type not found.");
    }
}
