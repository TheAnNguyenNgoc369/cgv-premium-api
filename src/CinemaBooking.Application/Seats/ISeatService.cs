using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Seats;

public interface ISeatService
{
    Task<List<Seat>?> GetSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<Seat?> GetSeatByIdAsync(
        int roomId,
        int seatId,
        CancellationToken cancellationToken = default);

    Task<SeatLayoutResult?> GetLayoutAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> CreateSeatAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        int seatTypeId,
        string status,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> UpdateSeatAsync(
        int roomId,
        int seatId,
        int seatTypeId,
        string status,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteSeatAsync(
        int roomId,
        int seatId,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, List<Seat> Seats)> ReplaceLayoutAsync(
        int roomId,
        int totalRows,
        int totalCols,
        IReadOnlyCollection<SeatLayoutSeatItem> seats,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);
}
