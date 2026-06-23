using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IMovieRepository
{
    Task<MovieEntity?> GetByIdAsync(int movieId, CancellationToken cancellationToken = default);

    Task<MovieEntity?> UpdatePosterAsync(
        int movieId,
        string? posterUrl,
        string? posterPublicId,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> GetMoviesByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);

    Task<MovieEntity?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default);
}
