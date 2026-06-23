using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Showtimes;

public interface IShowtimeService
{
    Task<List<Showtime>> GetShowtimesByMovieAsync(
        int movieId,
        DateOnly? date,
        int? cinemaId,
        CancellationToken cancellationToken = default);

    Task<Showtime?> GetShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<SeatMapResult?> GetSeatMapAsync(
        int showtimeId,
        CancellationToken cancellationToken = default);
}

