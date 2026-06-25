using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.SeatTypes;

public interface ISeatTypeService
{
    Task<List<SeatType>> GetSeatTypesAsync(
        CancellationToken cancellationToken = default);

    Task<SeatType?> GetSeatTypeByIdAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, SeatType? SeatType)> CreateSeatTypeAsync(
        string typeName,
        decimal extraPrice,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, SeatType? SeatType)> UpdateSeatTypeAsync(
        int seatTypeId,
        string typeName,
        decimal extraPrice,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteSeatTypeAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default);
}
