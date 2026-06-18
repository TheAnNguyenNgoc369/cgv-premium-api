using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Movies;

public sealed class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;

    public MovieService(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<List<Movie>> GetMoviesByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _movieRepository.GetMoviesByStatusAsync(status, cancellationToken);
    }

    public async Task<Movie?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _movieRepository.GetMovieByIdAsync(id, cancellationToken);
    }

    public async Task<List<Movie>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new List<Movie>();

        return await _movieRepository.SearchMoviesAsync(keyword, cancellationToken);
    }
}