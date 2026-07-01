using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IShowtimeRepository
{
    Task<(List<Showtime> Items, int TotalItems)> GetShowtimesAsync(
        int? movieId, int? cinemaId, string? movieName, string? roomName,
        DateOnly? date, string? status,
        bool onlyActiveLocations,
        int page, int pageSize, string sortBy, bool descending,
        CancellationToken cancellationToken = default);

    Task<CinemaBooking.Domain.Entities.Movie?> GetMovieAsync(
        int movieId, CancellationToken cancellationToken = default);
    Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(int roomId, DateTime startTime, DateTime endTime,
        int? excludingShowtimeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActiveBookingOrHoldAsync(int showtimeId, DateTime now,
        CancellationToken cancellationToken = default);
    Task<bool> HasAnyBookingOrHoldAsync(int showtimeId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlySet<int>> GetSoldOutShowtimeIdsAsync(
        IReadOnlyCollection<int> showtimeIds, DateTime now,
        CancellationToken cancellationToken = default);
    Task<bool> AcquireRoomScheduleLockAsync(
        int roomId, CancellationToken cancellationToken = default);
    Task<Showtime> AddAsync(Showtime showtime, CancellationToken cancellationToken = default);
    Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int showtimeId, CancellationToken cancellationToken = default);

    Task<Showtime?> GetShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Showtime?> GetManagedShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<Seat>> GetSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetBookedSeatIdsAsync(
        int showtimeId,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetHeldSeatIdsAsync(
        int showtimeId,
        DateTime now,
        CancellationToken cancellationToken = default);
}
