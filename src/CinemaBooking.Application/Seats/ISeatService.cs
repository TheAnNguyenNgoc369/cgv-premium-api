using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Seats;

public interface ISeatService
{
    Task<List<Seat>?> GetSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> CreateSeatAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        string? seatCode,
        string type,
        string status,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> UpdateSeatAsync(
        int roomId,
        int seatId,
        string type,
        string status,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteSeatAsync(
        int seatId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, List<Seat> Seats)> ReplaceLayoutAsync(
        int roomId,
        int totalRows,
        int seatsPerRow,
        string seatType,
        string seatStatus,
        CancellationToken cancellationToken = default);
}
