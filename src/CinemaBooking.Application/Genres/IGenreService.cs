using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Genres;

public interface IGenreService
{
    Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken = default);

    Task<Genre?> GetGenreByIdAsync(int genreId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Genre? Genre)> CreateGenreAsync(
        string genreName,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Genre? Genre)> UpdateGenreAsync(
        int genreId,
        string genreName,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteGenreAsync(
        int genreId,
        CancellationToken cancellationToken = default);
}
