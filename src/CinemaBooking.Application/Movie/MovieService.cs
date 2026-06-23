using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Interfaces;
using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.Application.Movie;

public sealed class MovieService : IMovieService
{
    private const string PosterFolder = "cgvp/movie-posters";

    private readonly IMovieRepository _movieRepository;
    private readonly IImageStorageService _imageStorageService;

    public MovieService(
        IMovieRepository movieRepository,
        IImageStorageService imageStorageService)
    {
        _movieRepository = movieRepository;
        _imageStorageService = imageStorageService;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> UploadPosterAsync(
        int movieId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        var validationError = ImageFileValidator.Validate(fileName, contentType, fileSize);

        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var movie = await _movieRepository.GetByIdAsync(movieId, cancellationToken);

        if (movie is null)
        {
            return (false, "Movie not found", null);
        }

        var previousPublicId = movie.PosterPublicId;
        var uploadResult = await _imageStorageService.UploadImageAsync(
            imageStream,
            fileName,
            PosterFolder,
            cancellationToken);

        var updatedMovie = await _movieRepository.UpdatePosterAsync(
            movieId,
            uploadResult.SecureUrl,
            uploadResult.PublicId,
            cancellationToken);

        if (updatedMovie is null)
        {
            await _imageStorageService.DeleteImageAsync(uploadResult.PublicId, cancellationToken);
            return (false, "Movie not found", null);
        }

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await _imageStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return (true, null, updatedMovie);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> DeletePosterAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var movie = await _movieRepository.GetByIdAsync(movieId, cancellationToken);

        if (movie is null)
        {
            return (false, "Movie not found", null);
        }

        var previousPublicId = movie.PosterPublicId;
        var updatedMovie = await _movieRepository.UpdatePosterAsync(
            movieId,
            null,
            null,
            cancellationToken);

        if (updatedMovie is null)
        {
            return (false, "Movie not found", null);
        }

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await _imageStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return (true, null, updatedMovie);
    }

    public Task<List<MovieEntity>> GetMoviesByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return _movieRepository.GetMoviesByStatusAsync(status, cancellationToken);
    }

    public Task<MovieEntity?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _movieRepository.GetMovieByIdAsync(id, cancellationToken);
    }

    public Task<List<MovieEntity>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Task.FromResult(new List<MovieEntity>());
        }

        return _movieRepository.SearchMoviesAsync(keyword, cancellationToken);
    }
}
