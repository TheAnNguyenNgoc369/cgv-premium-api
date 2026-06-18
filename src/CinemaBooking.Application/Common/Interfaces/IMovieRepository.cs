using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IMovieRepository
{
    Task<Movie?> GetByIdAsync(int movieId, CancellationToken cancellationToken = default);

    Task<Movie?> UpdatePosterAsync(
        int movieId,
        string? posterUrl,
        string? posterPublicId,
        CancellationToken cancellationToken = default);
}
