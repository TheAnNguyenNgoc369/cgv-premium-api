using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Movie;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
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
        IReadOnlyCollection<int> genreIds,
        int? cinemaId,
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

        if (genreIds.Count > 0)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => genreIds.Contains(mg.GenreID)));
        }

        if (cinemaId.HasValue)
        {
            query = query.Where(m => _dbContext.Showtimes.Any(s => s.MovieID == m.MovieID
                && s.Room.CinemaID == cinemaId.Value));
        }

        return query
            .OrderBy(m => m.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> CinemaExistsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Cinemas.AsNoTracking()
            .AnyAsync(c => c.CinemaID == cinemaId, cancellationToken);

    public Task<bool> TitleExistsAsync(
        string title,
        int? excludingMovieId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.ToUpper();
        var query = _dbContext.Movie.AsNoTracking()
            .Where(m => m.Title.ToUpper() == normalizedTitle);

        if (excludingMovieId.HasValue)
            query = query.Where(m => m.MovieID != excludingMovieId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public Task<List<MovieTicketSales>> GetMovieTicketSalesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Completed
                && payment.Booking.Status != BookingStatus.Cancelled
                && payment.Booking.Status != BookingStatus.Refunded)
            .SelectMany(payment => payment.Booking.BookingSeats)
            .GroupBy(bookingSeat => new
            {
                bookingSeat.Booking.Showtime.MovieID,
                bookingSeat.Booking.Showtime.Movie.Title
            })
            .Select(group => new MovieTicketSales(
                group.Key.MovieID,
                group.Key.Title,
                group.Sum(bookingSeat => bookingSeat.Seat.SeatType == null
                    ? 1
                    : bookingSeat.Seat.SeatType.Capacity)))
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
                && s.Status == "scheduled"
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
