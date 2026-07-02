using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Showtimes;

public interface IShowtimeService
{
    Task<(bool Succeeded, string? ErrorMessage, ShowtimePageResult? Page)> GetShowtimesAsync(
        int? movieId, int? cinemaId, string? movieName, string? roomName,
        DateOnly? date, string? status,
        int page, int pageSize, string? sortBy, string? sortDir,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? ErrorMessage, Showtime? Showtime)> CreateShowtimeAsync(
        int movieId, int roomId, DateTime startTime, decimal basePrice,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? ErrorMessage, Showtime? Showtime)> UpdateShowtimeAsync(
        int id, int movieId, int roomId, DateTime startTime, decimal basePrice, string? status,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? ErrorMessage)> DeleteShowtimeAsync(
        int id, int? managerCinemaId = null, CancellationToken cancellationToken = default);
    Task<bool> IsSoldOutAsync(Showtime showtime, CancellationToken cancellationToken = default);

    Task<Showtime?> GetShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Showtime?> GetManagedShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<(SeatMapResult? SeatMap, string? ErrorMessage)> GetSeatMapAsync(
        int showtimeId,
        CancellationToken cancellationToken = default);
}

