using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface ISeatTypeRepository
{
    Task<List<SeatType>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SeatType?> GetByIdAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string typeName,
        int? excludingSeatTypeId = null,
        CancellationToken cancellationToken = default);

    Task<SeatType> AddAsync(
        SeatType seatType,
        CancellationToken cancellationToken = default);

    Task<SeatType?> UpdateAsync(
        int seatTypeId,
        string typeName,
        decimal extraPrice,
        CancellationToken cancellationToken = default);

    Task<bool> IsUsedByAnySeatAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default);
}
