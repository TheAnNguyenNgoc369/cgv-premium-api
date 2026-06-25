using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Genres;

public sealed class GenreService : IGenreService
{
    private readonly IGenreRepository _genreRepository;

    public GenreService(IGenreRepository genreRepository)
    {
        _genreRepository = genreRepository;
    }

    public Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        return _genreRepository.GetGenresAsync(cancellationToken);
    }

    public Task<Genre?> GetGenreByIdAsync(
        int genreId,
        CancellationToken cancellationToken = default)
    {
        return _genreRepository.GetByIdAsync(genreId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Genre? Genre)> CreateGenreAsync(
        string genreName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(genreName);
        if (normalizedName is null)
        {
            return (false, "GenreName is required", null);
        }

        if (await _genreRepository.NameExistsAsync(normalizedName, cancellationToken: cancellationToken))
        {
            return (false, "GenreName must be unique", null);
        }

        var genre = new Genre
        {
            GenreName = normalizedName
        };

        var createdGenre = await _genreRepository.AddAsync(genre, cancellationToken);

        return (true, null, createdGenre);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Genre? Genre)> UpdateGenreAsync(
        int genreId,
        string genreName,
        CancellationToken cancellationToken = default)
    {
        var existingGenre = await _genreRepository.GetByIdAsync(genreId, cancellationToken);
        if (existingGenre is null)
        {
            return (false, "Genre not found", null);
        }

        var normalizedName = NormalizeName(genreName);
        if (normalizedName is null)
        {
            return (false, "GenreName is required", null);
        }

        if (await _genreRepository.NameExistsAsync(
                normalizedName,
                genreId,
                cancellationToken))
        {
            return (false, "GenreName must be unique", null);
        }

        var updatedGenre = await _genreRepository.UpdateAsync(
            genreId,
            normalizedName,
            cancellationToken);

        return updatedGenre is null
            ? (false, "Genre not found", null)
            : (true, null, updatedGenre);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteGenreAsync(
        int genreId,
        CancellationToken cancellationToken = default)
    {
        var existingGenre = await _genreRepository.GetByIdAsync(genreId, cancellationToken);
        if (existingGenre is null)
        {
            return (false, "Genre not found");
        }

        if (await _genreRepository.IsAssignedToAnyMovieAsync(genreId, cancellationToken))
        {
            return (false, "Genre is assigned to a movie");
        }

        var deleted = await _genreRepository.DeleteAsync(genreId, cancellationToken);

        return deleted
            ? (true, null)
            : (false, "Genre not found");
    }

    private static string? NormalizeName(string genreName)
    {
        return string.IsNullOrWhiteSpace(genreName)
            ? null
            : genreName.Trim();
    }
}
