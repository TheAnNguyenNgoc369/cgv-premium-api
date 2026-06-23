using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.Application.Movie;

public interface IMovieService
{
    Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> UploadPosterAsync(
        int movieId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> DeletePosterAsync(
        int movieId,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> GetMoviesByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);

    Task<MovieEntity?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<MovieEntity>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default);
}
