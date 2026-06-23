using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IShowtimeRepository
{
    Task<List<Showtime>> GetShowtimesByMovieAsync(
        int movieId,
        DateOnly? date,
        int? cinemaId,
        CancellationToken cancellationToken = default);

    Task<Showtime?> GetShowtimeByIdAsync(
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
        CancellationToken cancellationToken = default);
}