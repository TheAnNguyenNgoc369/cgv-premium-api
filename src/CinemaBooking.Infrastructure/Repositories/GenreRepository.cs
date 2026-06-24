using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class GenreRepository : IGenreRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public GenreRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Genres
            .AsNoTracking()
            .OrderBy(g => g.GenreName)
            .ToListAsync(cancellationToken);
    }

    public Task<Genre?> GetByIdAsync(
        int genreId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Genres
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GenreID == genreId, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string genreName,
        int? excludingGenreId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Genres
            .AsNoTracking()
            .Where(g => g.GenreName == genreName);

        if (excludingGenreId.HasValue)
        {
            query = query.Where(g => g.GenreID != excludingGenreId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<Genre> AddAsync(
        Genre genre,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Genres.Add(genre);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return genre;
    }

    public async Task<Genre?> UpdateAsync(
        int genreId,
        string genreName,
        CancellationToken cancellationToken = default)
    {
        var genre = await _dbContext.Genres
            .FirstOrDefaultAsync(g => g.GenreID == genreId, cancellationToken);

        if (genre is null)
        {
            return null;
        }

        genre.GenreName = genreName;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return genre;
    }

    public Task<bool> IsAssignedToAnyMovieAsync(
        int genreId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MovieGenres
            .AnyAsync(mg => mg.GenreID == genreId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int genreId,
        CancellationToken cancellationToken = default)
    {
        var genre = await _dbContext.Genres
            .FirstOrDefaultAsync(g => g.GenreID == genreId, cancellationToken);

        if (genre is null)
        {
            return false;
        }

        _dbContext.Genres.Remove(genre);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
