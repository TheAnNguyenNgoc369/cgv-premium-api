using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IMovieRepository
{
    Task<MovieEntity?> GetByIdAsync(int movieId, CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> GetMoviesAsync(
        string? status,
        CancellationToken cancellationToken = default);

    Task<MovieEntity?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default);

    Task<MovieEntity> AddMovieAsync(
        MovieEntity movie,
        IReadOnlyCollection<string> genreNames,
        CancellationToken cancellationToken = default);

    Task<MovieEntity?> UpdateMovieAsync(
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
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveOrUpcomingShowtimesAsync(
        int movieId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteMovieAsync(
        int movieId,
        CancellationToken cancellationToken = default);
}
