using MovieEntity = CinemaBooking.Domain.Entities.Movie;
using CinemaBooking.Application.Common.Interfaces;

namespace CinemaBooking.Application.Movie;

public interface IMovieService
{
    Task<List<MovieEntity>> GetMoviesAsync(
        string? status,
        IReadOnlyCollection<int> genreIds,
        CancellationToken cancellationToken = default);

    Task<MovieEntity?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> CreateMovieAsync(
        string title,
        List<string>? genres,
        string? ageRating,
        string director,
        string? cast,
        string? synopsis,
        int durationMinutes,
        DateOnly? showingFromDate,
        DateOnly? showingToDate,
        string? posterUrl,
        string? posterPublicId,
        string? trailerUrl,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> UpdateMovieAsync(
        int movieId,
        string title,
        List<string>? genres,
        string? ageRating,
        string director,
        string? cast,
        string? synopsis,
        int durationMinutes,
        DateOnly? showingFromDate,
        DateOnly? showingToDate,
        string? posterUrl,
        string? posterPublicId,
        string? trailerUrl,
        string? status,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> UpdatePosterAsync(
        int movieId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteMovieAsync(
        int movieId,
        CancellationToken cancellationToken = default);
}
