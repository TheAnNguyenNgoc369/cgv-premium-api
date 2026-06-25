using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IGenreRepository
{
    Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken = default);

    Task<Genre?> GetByIdAsync(int genreId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string genreName,
        int? excludingGenreId = null,
        CancellationToken cancellationToken = default);

    Task<Genre> AddAsync(Genre genre, CancellationToken cancellationToken = default);

    Task<Genre?> UpdateAsync(
        int genreId,
        string genreName,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToAnyMovieAsync(
        int genreId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int genreId, CancellationToken cancellationToken = default);
}
