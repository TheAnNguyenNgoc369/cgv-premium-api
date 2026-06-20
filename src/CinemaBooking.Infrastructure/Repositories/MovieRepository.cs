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
        return _dbContext.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);
    }

    public async Task<Movie?> UpdatePosterAsync(
        int movieId,
        string? posterUrl,
        string? posterPublicId,
        CancellationToken cancellationToken = default)
    {
        var movie = await _dbContext.Movies
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
}
