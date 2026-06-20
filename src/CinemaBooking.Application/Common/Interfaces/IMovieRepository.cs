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
}
