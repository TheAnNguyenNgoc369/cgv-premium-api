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
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Movie
            .AsNoTracking()
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .Include(m => m.MoviePersons)
            .ThenInclude(mp => mp.Person)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (genreIds.Count > 0)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => genreIds.Contains(mg.GenreID)));
        }

        return query
            .OrderBy(m => m.Title)
            .ToListAsync(cancellationToken);
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
            .Include(m => m.MoviePersons)
            .ThenInclude(mp => mp.Person)
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
            .Include(m => m.MoviePersons)
            .ThenInclude(mp => mp.Person)
            .Where(m => m.Title.Contains(keyword))
            .ToListAsync(cancellationToken);
    }

    public async Task<(Movie? Movie, IReadOnlyList<int> MissingPersonIds)> AddMovieAsync(
        Movie movie,
        IReadOnlyCollection<string> genreNames,
        IReadOnlyList<int> directorIds,
        IReadOnlyList<int> actorIds,
        CancellationToken cancellationToken = default)
    {
        var missingPersonIds = await FindMissingPersonIdsAsync(directorIds, actorIds, cancellationToken);
        if (missingPersonIds.Count > 0)
        {
            return (null, missingPersonIds);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(Movie? Movie, IReadOnlyList<int> MissingPersonIds)>(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            movie.MovieGenres = await BuildMovieGenresAsync(genreNames, cancellationToken);
            movie.MoviePersons = BuildMoviePersonRows(directorIds, actorIds);

            _dbContext.Movie.Add(movie);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var reloaded = await GetMovieByIdAsync(movie.MovieID, cancellationToken) ?? movie;
            return (reloaded, Array.Empty<int>());
        });
    }

    public async Task<(Movie? Movie, IReadOnlyList<int> MissingPersonIds)> UpdateMovieAsync(
        int movieId,
        string title,
        IReadOnlyCollection<string>? genreNames,
        string ageRating,
        IReadOnlyList<int> directorIds,
        IReadOnlyList<int> actorIds,
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
        var missingPersonIds = await FindMissingPersonIdsAsync(directorIds, actorIds, cancellationToken);
        if (missingPersonIds.Count > 0)
        {
            return (null, missingPersonIds);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(Movie? Movie, IReadOnlyList<int> MissingPersonIds)>(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var movie = await _dbContext.Movie
                .Include(m => m.MovieGenres)
                .Include(m => m.MoviePersons)
                .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);

            if (movie is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (null, Array.Empty<int>());
            }

            movie.Title = title;
            movie.AgeRating = ageRating;
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

            _dbContext.MoviePersons.RemoveRange(movie.MoviePersons);
            movie.MoviePersons = BuildMoviePersonRows(directorIds, actorIds);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var reloaded = await GetMovieByIdAsync(movieId, cancellationToken);
            return (reloaded, Array.Empty<int>());
        });
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
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var movie = await _dbContext.Movie
                .Include(m => m.MovieGenres)
                .Include(m => m.MoviePersons)
                .FirstOrDefaultAsync(m => m.MovieID == movieId, cancellationToken);

            if (movie is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            _dbContext.MovieGenres.RemoveRange(movie.MovieGenres);
            _dbContext.MoviePersons.RemoveRange(movie.MoviePersons);
            _dbContext.Movie.Remove(movie);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
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

    private async Task<IReadOnlyList<int>> FindMissingPersonIdsAsync(
        IReadOnlyList<int> directorIds,
        IReadOnlyList<int> actorIds,
        CancellationToken cancellationToken)
    {
        var allIds = directorIds
            .Concat(actorIds)
            .Distinct()
            .ToList();

        if (allIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        var existingIds = await _dbContext.Persons
            .Where(p => allIds.Contains(p.PersonId))
            .Select(p => p.PersonId)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<int>(existingIds);
        var missing = allIds.Where(id => !existingSet.Contains(id)).ToList();
        return missing;
    }

    private static List<MoviePerson> BuildMoviePersonRows(
        IReadOnlyList<int> directorIds,
        IReadOnlyList<int> actorIds)
    {
        var rows = new List<MoviePerson>(directorIds.Count + actorIds.Count);
        var seen = new HashSet<(int PersonId, string Role)>();

        for (var index = 0; index < directorIds.Count; index++)
        {
            var personId = directorIds[index];
            if (!seen.Add((personId, MoviePersonRoles.Director)))
            {
                continue;
            }

            rows.Add(new MoviePerson
            {
                PersonId = personId,
                Role = MoviePersonRoles.Director,
                DisplayOrder = index
            });
        }

        for (var index = 0; index < actorIds.Count; index++)
        {
            var personId = actorIds[index];
            if (!seen.Add((personId, MoviePersonRoles.Actor)))
            {
                continue;
            }

            rows.Add(new MoviePerson
            {
                PersonId = personId,
                Role = MoviePersonRoles.Actor,
                DisplayOrder = index
            });
        }

        return rows;
    }
}
