using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class MovieRepository : IMovieRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public MovieRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Movie?> GetByIdAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Movie
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);
    }

    public Task<List<Movie>> GetMoviesAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Movie
            .AsNoTracking()
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == status);
        }

        return query
            .OrderBy(m => m.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<Movie?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Movie
            .AsNoTracking()
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.MovieID == id, cancellationToken);
    }

    public Task<List<Movie>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Movie
            .AsNoTracking()
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .Where(m => m.Title.Contains(keyword))
            .ToListAsync(cancellationToken);
    }

    public async Task<Movie> AddMovieAsync(
        Movie movie,
        IReadOnlyCollection<string> genreNames,
        CancellationToken cancellationToken = default)
    {
        movie.MovieGenres = await BuildMovieGenresAsync(genreNames, cancellationToken);

        _dbContext.Movie.Add(movie);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMovieByIdAsync(movie.MovieID, cancellationToken) ?? movie;
    }

    public async Task<Movie?> UpdateMovieAsync(
        int movieId,
        string title,
        IReadOnlyCollection<string>? genreNames,
        string ageRating,
        string director,
        string? cast,
        string? synopsis,
        int durationMinutes,
        DateOnly? showingFromDate,
        DateOnly? showingToDate,
        string? posterUrl,
        string? posterPublicId,
        string? trailerUrl,
        string status,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var movie = await _dbContext.Movie
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);

        if (movie is null)
        {
            return null;
        }

        movie.Title = title;
        movie.AgeRating = ageRating;
        movie.Director = director;
        movie.Cast = cast;
        movie.Description = synopsis;
        movie.DurationMin = durationMinutes;
        movie.ShowingFrom = showingFromDate;
        movie.ShowingTo = showingToDate;
        movie.PosterURL = posterUrl;
        movie.PosterPublicId = posterPublicId;
        movie.TrailerURL = trailerUrl;
        movie.Status = status;
        movie.UpdatedAt = updatedAt;

        if (genreNames is not null)
        {
            _dbContext.MovieGenres.RemoveRange(movie.MovieGenres);
            movie.MovieGenres = await BuildMovieGenresAsync(genreNames, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMovieByIdAsync(movieId, cancellationToken);
    }

    public async Task<Movie?> UpdatePosterAsync(
        int movieId,
        string posterUrl,
        string posterPublicId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var movie = await _dbContext.Movie
            .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);
        if (movie is null)
            return null;

        movie.PosterURL = posterUrl;
        movie.PosterPublicId = posterPublicId;
        movie.UpdatedAt = updatedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetMovieByIdAsync(movieId, cancellationToken);
    }

    public Task<bool> HasActiveOrUpcomingShowtimesAsync(
        int movieId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Showtimes
            .AnyAsync(s => s.MovieID == movieId
                && (s.Status == "scheduled" || s.Status == "ongoing")
                && s.EndTime >= now, cancellationToken);
    }

    public async Task<bool> DeleteMovieAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var movie = await _dbContext.Movie
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);

        if (movie is null)
        {
            return false;
        }

        _dbContext.MovieGenres.RemoveRange(movie.MovieGenres);
        _dbContext.Movie.Remove(movie);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<List<MovieGenre>> BuildMovieGenresAsync(
        IReadOnlyCollection<string> genreNames,
        CancellationToken cancellationToken)
    {
        if (genreNames.Count == 0)
        {
            return [];
        }

        var existingGenres = await _dbContext.Genres
            .Where(g => genreNames.Contains(g.GenreName))
            .ToListAsync(cancellationToken);

        var existingGenreNames = existingGenres
            .Select(g => g.GenreName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingGenres = genreNames
            .Where(genreName => !existingGenreNames.Contains(genreName))
            .Select(genreName => new Genre { GenreName = genreName })
            .ToList();

        if (missingGenres.Count > 0)
        {
            _dbContext.Genres.AddRange(missingGenres);
        }

        return existingGenres
            .Concat(missingGenres)
            .Select(genre => new MovieGenre { Genre = genre })
            .ToList();
    }
}
