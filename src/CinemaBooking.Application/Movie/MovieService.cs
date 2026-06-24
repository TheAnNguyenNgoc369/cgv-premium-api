using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Interfaces;
using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.Application.Movie;

public sealed class MovieService : IMovieService
{
    private const string PosterFolder = "cgvp/movie-posters";
    private const string PosterUploadFailedMessage = "Movie poster could not be uploaded. Please try again later.";
    private const string PosterDeleteFailedMessage = "Movie poster could not be deleted. Please try again later.";

    private static readonly HashSet<string> ValidAgeRatings = new(StringComparer.Ordinal)
    {
        "P",
        "C13",
        "C16",
        "C18"
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "coming_soon",
        "now_showing",
        "ended"
    };

    private readonly IMovieRepository _movieRepository;
    private readonly IImageStorageService _imageStorageService;

    public MovieService(
        IMovieRepository movieRepository,
        IImageStorageService imageStorageService)
    {
        _movieRepository = movieRepository;
        _imageStorageService = imageStorageService;
    }

    public Task<List<MovieEntity>> GetMoviesAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeOptionalStatus(status);

        return normalizedStatus == InvalidStatus
            ? Task.FromResult(new List<MovieEntity>())
            : _movieRepository.GetMoviesAsync(normalizedStatus, cancellationToken);
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

    public async Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> CreateMovieAsync(
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
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateMovie(
            title,
            ageRating,
            director,
            durationMinutes,
            showingFromDate,
            showingToDate,
            status,
            defaultStatus: true);

        if (!validation.Succeeded)
        {
            return (false, validation.ErrorMessage, null);
        }

        var posterValidationError = ValidatePosterMetadata(posterUrl, posterPublicId);
        if (posterValidationError is not null)
            return (false, posterValidationError, null);

        var now = DateTime.UtcNow;
        var movie = new MovieEntity
        {
            Title = title.Trim(),
            AgeRating = ageRating!.Trim().ToUpperInvariant(),
            Director = director.Trim(),
            Cast = NormalizeNullable(cast),
            Description = NormalizeNullable(synopsis),
            DurationMin = durationMinutes,
            ShowingFrom = showingFromDate,
            ShowingTo = showingToDate,
            PosterURL = NormalizeNullable(posterUrl),
            PosterPublicId = NormalizeNullable(posterPublicId),
            TrailerURL = NormalizeNullable(trailerUrl),
            Status = validation.Status!,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createdMovie = await _movieRepository.AddMovieAsync(
            movie,
            NormalizeGenres(genres),
            cancellationToken);
        return (true, null, createdMovie);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> UpdateMovieAsync(
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
        CancellationToken cancellationToken = default)
    {
        var existingMovie = await _movieRepository.GetByIdAsync(movieId, cancellationToken);
        if (existingMovie is null)
        {
            return (false, "Movie not found", null);
        }

        var validation = ValidateMovie(
            title,
            ageRating,
            director,
            durationMinutes,
            showingFromDate,
            showingToDate,
            status,
            defaultStatus: false);

        if (!validation.Succeeded)
        {
            return (false, validation.ErrorMessage, null);
        }

        var posterValidationError = ValidatePosterMetadata(posterUrl, posterPublicId);
        if (posterValidationError is not null)
            return (false, posterValidationError, null);

        var normalizedPosterUrl = NormalizeNullable(posterUrl);
        var normalizedPosterPublicId = NormalizeNullable(posterPublicId);
        var previousPosterPublicId = existingMovie.PosterPublicId;
        var shouldDeletePreviousPosterAfterUpdate =
            !string.IsNullOrWhiteSpace(previousPosterPublicId)
            && !string.Equals(previousPosterPublicId, normalizedPosterPublicId, StringComparison.Ordinal);

        if (shouldDeletePreviousPosterAfterUpdate)
        {
            try
            {
                await _imageStorageService.DeleteImageAsync(previousPosterPublicId!, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return (false, PosterDeleteFailedMessage, null);
            }
        }

        var updatedMovie = await _movieRepository.UpdateMovieAsync(
            movieId,
            title.Trim(),
            genres is null ? null : NormalizeGenres(genres),
            ageRating!.Trim().ToUpperInvariant(),
            director.Trim(),
            NormalizeNullable(cast),
            NormalizeNullable(synopsis),
            durationMinutes,
            showingFromDate,
            showingToDate,
            normalizedPosterUrl,
            normalizedPosterPublicId,
            NormalizeNullable(trailerUrl),
            validation.Status!,
            DateTime.UtcNow,
            cancellationToken);

        if (updatedMovie is null)
        {
            return (false, "Movie not found", null);
        }

        return (true, null, updatedMovie);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, MovieEntity? Movie)> UpdatePosterAsync(
        int movieId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        var existingMovie = await _movieRepository.GetByIdAsync(movieId, cancellationToken);
        if (existingMovie is null)
            return (false, "Movie not found", null);

        var validationError = ImageFileValidator.Validate(fileName, contentType, fileSize);
        if (validationError is not null)
            return (false, validationError, null);

        StoredImageResult newPoster;
        try
        {
            newPoster = await _imageStorageService.UploadImageAsync(
                imageStream, fileName, PosterFolder, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (false, PosterUploadFailedMessage, null);
        }

        if (!string.IsNullOrWhiteSpace(existingMovie.PosterPublicId))
        {
            try
            {
                await _imageStorageService.DeleteImageAsync(
                    existingMovie.PosterPublicId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                try
                {
                    await _imageStorageService.DeleteImageAsync(
                        newPoster.PublicId, CancellationToken.None);
                }
                catch (Exception cleanupException) when (cleanupException is not OperationCanceledException)
                {
                }

                return (false, PosterDeleteFailedMessage, null);
            }
        }

        try
        {
            var updatedMovie = await _movieRepository.UpdatePosterAsync(
                movieId,
                newPoster.SecureUrl,
                newPoster.PublicId,
                DateTime.UtcNow,
                cancellationToken);
            return updatedMovie is null
                ? (false, "Movie not found", null)
                : (true, null, updatedMovie);
        }
        catch
        {
            try
            {
                await _imageStorageService.DeleteImageAsync(
                    newPoster.PublicId, CancellationToken.None);
            }
            catch (Exception cleanupException) when (cleanupException is not OperationCanceledException)
            {
            }

            throw;
        }
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteMovieAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var existingMovie = await _movieRepository.GetByIdAsync(movieId, cancellationToken);
        if (existingMovie is null)
        {
            return (false, "Movie not found");
        }

        if (await _movieRepository.HasActiveOrUpcomingShowtimesAsync(
                movieId,
                DateTime.UtcNow,
                cancellationToken))
        {
            return (false, "Movie has active or upcoming showtimes");
        }

        if (!string.IsNullOrWhiteSpace(existingMovie.PosterPublicId))
        {
            try
            {
                await _imageStorageService.DeleteImageAsync(existingMovie.PosterPublicId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return (false, PosterDeleteFailedMessage);
            }
        }

        var deleted = await _movieRepository.DeleteMovieAsync(movieId, cancellationToken);

        if (!deleted)
        {
            return (false, "Movie not found");
        }

        return (true, null);
    }

    private const string InvalidStatus = "__invalid_status__";

    private static (bool Succeeded, string? ErrorMessage, string? Status) ValidateMovie(
        string title,
        string? ageRating,
        string director,
        int durationMinutes,
        DateOnly? showingFromDate,
        DateOnly? showingToDate,
        string? status,
        bool defaultStatus)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, "Title is required", null);
        }

        if (string.IsNullOrWhiteSpace(director))
        {
            return (false, "Director is required", null);
        }

        if (durationMinutes <= 0)
        {
            return (false, "DurationMinutes must be greater than 0", null);
        }

        if (string.IsNullOrWhiteSpace(ageRating))
        {
            return (false, "AgeRating is required", null);
        }

        var normalizedAgeRating = ageRating.Trim().ToUpperInvariant();
        if (!ValidAgeRatings.Contains(normalizedAgeRating))
        {
            return (false, "AgeRating must be P, C13, C16, or C18", null);
        }

        if (showingFromDate.HasValue
            && showingToDate.HasValue
            && showingFromDate.Value > showingToDate.Value)
        {
            return (false, "ShowingFromDate must be before or equal to ShowingToDate", null);
        }

        var normalizedStatus = NormalizeStatus(status, defaultStatus);
        if (normalizedStatus == InvalidStatus)
        {
            return (false, "Status must be coming_soon, now_showing, or ended", null);
        }

        return (true, null, normalizedStatus);
    }

    private static string NormalizeStatus(string? status, bool defaultToComingSoon)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return defaultToComingSoon ? "coming_soon" : InvalidStatus;
        }

        var normalizedStatus = status.Trim();

        return ValidStatuses.Contains(normalizedStatus)
            ? normalizedStatus
            : InvalidStatus;
    }

    private static string? NormalizeOptionalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalizedStatus = status.Trim();

        return ValidStatuses.Contains(normalizedStatus)
            ? normalizedStatus
            : InvalidStatus;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? ValidatePosterMetadata(string? posterUrl, string? posterPublicId)
    {
        var hasUrl = !string.IsNullOrWhiteSpace(posterUrl);
        var hasPublicId = !string.IsNullOrWhiteSpace(posterPublicId);
        return hasUrl == hasPublicId
            ? null
            : "PosterUrl and PosterPublicId must be provided together";
    }

    private static IReadOnlyCollection<string> NormalizeGenres(List<string>? genres)
    {
        return genres?
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }
}
