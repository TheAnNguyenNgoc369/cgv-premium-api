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

    public async Task<Movie?> UpdatePosterAsync(
        int movieId,
        string? posterUrl,
        string? posterPublicId,
        CancellationToken cancellationToken = default)
    {
        var movie = await _dbContext.Movie
            .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);

        if (movie is null)
        {
            return null;
        }

        movie.PosterURL = posterUrl;
        movie.PosterPublicId = posterPublicId;
        movie.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return movie;
    }

    public Task<List<Movie>> GetMoviesByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Movie
            .AsNoTracking()
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.ShowingFrom)
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
}
