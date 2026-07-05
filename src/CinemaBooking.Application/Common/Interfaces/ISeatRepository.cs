using CinemaBooking.Application.Seats;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface ISeatRepository
{
    Task<List<Seat>> GetSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<Room?> GetRoomByIdAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<Seat?> GetSeatByIdAsync(
        int roomId,
        int seatId,
        CancellationToken cancellationToken = default);

    Task<Seat?> GetSeatByIdAsync(
        int seatId,
        CancellationToken cancellationToken = default);

    Task<SeatType?> GetSeatTypeByNameAsync(
        string typeName,
        CancellationToken cancellationToken = default);

    Task<SeatType?> GetSeatTypeByIdAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default);

    Task<int> CountSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<bool> SeatPositionExistsAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        int? excludingSeatId = null,
        CancellationToken cancellationToken = default);

    Task<List<Seat>> GetSeatsBySelectorAsync(
        int roomId,
        IReadOnlyCollection<SeatSelector> selectors,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveOrUpcomingShowtimesAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<bool> HasSeatRelationsAsync(
        int seatId,
        CancellationToken cancellationToken = default);

    Task<Seat?> AddAsync(
        Seat seat,
        CancellationToken cancellationToken = default);

    Task<Seat?> UpdateAsync(
        int roomId,
        int seatId,
        int? seatTypeId,
        string status,
        bool isGap,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int seatId,
        CancellationToken cancellationToken = default);

    Task<List<Seat>> ReplaceLayoutAsync(
        int roomId,
        IReadOnlyCollection<Seat> seats,
        CancellationToken cancellationToken = default);
}
