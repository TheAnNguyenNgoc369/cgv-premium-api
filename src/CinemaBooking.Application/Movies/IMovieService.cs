using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Movies;

public interface IMovieService
{
    Task<List<Movie>> GetMoviesByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);

    Task<Movie?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<Movie>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default);
}