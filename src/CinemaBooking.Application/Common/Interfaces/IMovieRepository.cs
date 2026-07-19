using MovieEntity = CinemaBooking.Domain.Entities.Movie;
using CinemaBooking.Application.Movie;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IMovieRepository
{
    Task<MovieEntity?> GetByIdAsync(int movieId, CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> GetMoviesAsync(
        string? status,
        IReadOnlyCollection<int> genreIds,
        int? cinemaId,
        CancellationToken cancellationToken = default);

    Task<bool> CinemaExistsAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<bool> TitleExistsAsync(
        string title,
        int? excludingMovieId = null,
        CancellationToken cancellationToken = default);

    Task<List<MovieTicketSales>> GetMovieTicketSalesAsync(
        CancellationToken cancellationToken = default);

    Task<MovieEntity?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default);

    Task<(MovieEntity? Movie, IReadOnlyList<int> MissingPersonIds)> AddMovieAsync(
        MovieEntity movie,
        IReadOnlyCollection<string> genreNames,
        IReadOnlyList<int> directorIds,
        IReadOnlyList<int> actorIds,
        CancellationToken cancellationToken = default);

    Task<(MovieEntity? Movie, IReadOnlyList<int> MissingPersonIds)> UpdateMovieAsync(
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
        CancellationToken cancellationToken = default);

    Task<MovieEntity?> UpdatePosterAsync(
        int movieId,
        string posterUrl,
        string posterPublicId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveOrUpcomingShowtimesAsync(
        int movieId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteMovieAsync(
        int movieId,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> GetMoviesByIdsAsync(
        List<int> movieIds,
        CancellationToken cancellationToken = default);
}
