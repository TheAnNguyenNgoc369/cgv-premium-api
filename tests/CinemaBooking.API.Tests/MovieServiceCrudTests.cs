using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Movie;
using CinemaBooking.Domain.Entities;
using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.API.Tests;

public sealed class MovieServiceCrudTests
{
    [Fact]
    public async Task CreateMovieDefaultsStatusAndNormalizesGenres()
    {
        var repository = new MovieRepositoryFake();
        var service = CreateService(repository);

        var result = await service.CreateMovieAsync(
            " Avatar ",
            [" Action ", "action", "Sci-Fi"],
            "p",
            " James Cameron ",
            "Cast",
            "Synopsis",
            120,
            null,
            null,
            null,
            null,
            null,
            null);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Movie);
        Assert.Equal("Avatar", result.Movie.Title);
        Assert.Equal("P", result.Movie.AgeRating);
        Assert.Equal("coming_soon", result.Movie.Status);
        Assert.Equal(["Action", "Sci-Fi"], repository.LastGenreNames);
    }

    [Fact]
    public async Task UpdatePosterStoresNewPosterMetadata()
    {
        var repository = new MovieRepositoryFake();
        var storage = new ImageStorageServiceFake
        {
            UploadResult = new StoredImageResult("https://example.com/poster.png", "poster-public-id")
        };
        var service = CreateService(repository, storage);

        repository.Movies.Add(CreateMovie(1));
        var result = await service.UpdatePosterAsync(
            1, new MemoryStream([1, 2, 3]), "poster.png", "image/png", 3);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Movie);
        Assert.Equal("https://example.com/poster.png", result.Movie.PosterURL);
        Assert.Equal("poster-public-id", result.Movie.PosterPublicId);
        Assert.Equal("poster.png", storage.UploadedFileName);
    }

    [Fact]
    public async Task CreateMovieRejectsMissingRequiredFields()
    {
        var repository = new MovieRepositoryFake();
        var service = CreateService(repository);

        var result = await service.CreateMovieAsync(
            "",
            null,
            "P",
            "Director",
            null,
            null,
            120,
            null,
            null,
            null,
            null,
            null,
            "coming_soon");

        Assert.False(result.Succeeded);
        Assert.Equal("Title is required", result.ErrorMessage);
        Assert.Null(result.Movie);
        Assert.Empty(repository.Movies);
    }

    [Fact]
    public async Task CreateMovieStoresUploadedPosterMetadata()
    {
        var repository = new MovieRepositoryFake();
        var service = CreateService(repository);

        var result = await service.CreateMovieAsync(
            "Avatar", null, "P", "Director", null, null, 120,
            null, null, "https://example.com/poster.png", "poster-public-id",
            null, "coming_soon");

        Assert.True(result.Succeeded);
        Assert.Equal("https://example.com/poster.png", result.Movie!.PosterURL);
        Assert.Equal("poster-public-id", result.Movie.PosterPublicId);
    }

    [Fact]
    public async Task CreateMovieRejectsIncompletePosterMetadata()
    {
        var service = CreateService(new MovieRepositoryFake());

        var result = await service.CreateMovieAsync(
            "Avatar", null, "P", "Director", null, null, 120,
            null, null, "https://example.com/poster.png", null,
            null, "coming_soon");

        Assert.False(result.Succeeded);
        Assert.Equal("PosterUrl and PosterPublicId must be provided together", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateMovieRejectsInvalidStatus()
    {
        var repository = new MovieRepositoryFake();
        var service = CreateService(repository);

        var result = await service.CreateMovieAsync(
            "Avatar",
            null,
            "P",
            "Director",
            null,
            null,
            120,
            null,
            null,
            null,
            null,
            null,
            "NOW_SHOWING");

        Assert.False(result.Succeeded);
        Assert.Equal("Status must be coming_soon, now_showing, or ended", result.ErrorMessage);
        Assert.Null(result.Movie);
    }

    [Fact]
    public async Task UpdateMovieRejectsMissingMovie()
    {
        var repository = new MovieRepositoryFake();
        var service = CreateService(repository);

        var result = await service.UpdateMovieAsync(
            404,
            "Avatar",
            null,
            "P",
            "Director",
            null,
            null,
            120,
            null,
            null,
            null,
            null,
            null,
            "now_showing");

        Assert.False(result.Succeeded);
        Assert.Equal("Movie not found", result.ErrorMessage);
        Assert.Null(result.Movie);
    }

    [Fact]
    public async Task UpdateMovieWithNewPosterMetadataDeletesOldPoster()
    {
        var movie = CreateMovie(1);
        movie.PosterURL = "https://example.com/old.png";
        movie.PosterPublicId = "old-public-id";
        var repository = new MovieRepositoryFake
        {
            Movies = { movie }
        };
        var storage = new ImageStorageServiceFake
        {
            UploadResult = new StoredImageResult("https://example.com/new.png", "new-public-id")
        };
        var service = CreateService(repository, storage);

        var result = await service.UpdateMovieAsync(
            1,
            "Avatar 2",
            null,
            "P",
            "Director",
            null,
            null,
            130,
            null,
            null,
            "https://example.com/new.png",
            "new-public-id",
            null,
            "now_showing");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Movie);
        Assert.Equal("https://example.com/new.png", result.Movie.PosterURL);
        Assert.Equal("new-public-id", result.Movie.PosterPublicId);
        Assert.Contains("old-public-id", storage.DeletedPublicIds);
        Assert.DoesNotContain("new-public-id", storage.DeletedPublicIds);
    }

    [Fact]
    public async Task UpdateMovieWithNewPosterMetadataDeletesOldPosterAsset()
    {
        var movie = CreateMovie(1);
        movie.PosterURL = "https://example.com/old.png";
        movie.PosterPublicId = "old-public-id";
        var repository = new MovieRepositoryFake
        {
            Movies = { movie }
        };
        var storage = new ImageStorageServiceFake();
        var service = CreateService(repository, storage);

        var result = await service.UpdateMovieAsync(
            1,
            "Avatar 2",
            null,
            "P",
            "Director",
            null,
            null,
            130,
            null,
            null,
            "https://example.com/new-external.png",
            "new-public-id",
            null,
            "now_showing");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Movie);
        Assert.Equal("https://example.com/new-external.png", result.Movie.PosterURL);
        Assert.Equal("new-public-id", result.Movie.PosterPublicId);
        Assert.Contains("old-public-id", storage.DeletedPublicIds);
    }

    [Fact]
    public async Task UpdateMovieWithSamePosterUrlKeepsOldPosterAsset()
    {
        var movie = CreateMovie(1);
        movie.PosterURL = "https://example.com/old.png";
        movie.PosterPublicId = "old-public-id";
        var repository = new MovieRepositoryFake
        {
            Movies = { movie }
        };
        var storage = new ImageStorageServiceFake();
        var service = CreateService(repository, storage);

        var result = await service.UpdateMovieAsync(
            1,
            "Avatar 2",
            null,
            "P",
            "Director",
            null,
            null,
            130,
            null,
            null,
            "https://example.com/old.png",
            "old-public-id",
            null,
            "now_showing");

        Assert.True(result.Succeeded);
        Assert.Empty(storage.DeletedPublicIds);
    }

    [Fact]
    public async Task UpdatePosterReturnsFailureWhenStorageFails()
    {
        var movie = CreateMovie(1);
        movie.PosterURL = "https://example.com/old.png";
        movie.PosterPublicId = "old-public-id";
        var repository = new MovieRepositoryFake
        {
            Movies = { movie }
        };
        var storage = new ImageStorageServiceFake
        {
            UploadFailure = new InvalidOperationException("Cloudinary upload failed")
        };
        var service = CreateService(repository, storage);

        var result = await service.UpdatePosterAsync(
            1, new MemoryStream([1, 2, 3]), "poster.png", "image/png", 3);

        Assert.False(result.Succeeded);
        Assert.Equal("Movie poster could not be uploaded. Please try again later.", result.ErrorMessage);
        Assert.Null(result.Movie);
    }

    [Fact]
    public async Task UpdatePosterRollsBackNewPosterWhenOldPosterDeletionFails()
    {
        var movie = CreateMovie(1);
        movie.PosterURL = "https://example.com/old.png";
        movie.PosterPublicId = "old-public-id";
        var repository = new MovieRepositoryFake { Movies = { movie } };
        var storage = new ImageStorageServiceFake
        {
            UploadResult = new("https://example.com/new.png", "new-public-id"),
            DeleteFailures =
            {
                ["old-public-id"] = new InvalidOperationException("Delete failed")
            }
        };
        var service = CreateService(repository, storage);

        var result = await service.UpdatePosterAsync(
            1, new MemoryStream([1, 2, 3]), "poster.png", "image/png", 3);

        Assert.False(result.Succeeded);
        Assert.Equal("Movie poster could not be deleted. Please try again later.", result.ErrorMessage);
        Assert.Equal("https://example.com/old.png", movie.PosterURL);
        Assert.Equal("old-public-id", movie.PosterPublicId);
        Assert.Contains("new-public-id", storage.DeletedPublicIds);
    }

    [Fact]
    public async Task UpdateMovieStopsWhenOldPosterDeletionFails()
    {
        var movie = CreateMovie(1);
        movie.PosterURL = "https://example.com/old.png";
        movie.PosterPublicId = "old-public-id";
        var repository = new MovieRepositoryFake
        {
            Movies = { movie },
        };
        var storage = new ImageStorageServiceFake
        {
            DeleteFailures =
            {
                ["old-public-id"] = new InvalidOperationException("Cloudinary delete failed")
            }
        };
        var service = CreateService(repository, storage);

        var result = await service.UpdateMovieAsync(
            1,
            "Avatar 2",
            null,
            "P",
            "Director",
            null,
            null,
            130,
            null,
            null,
            "https://example.com/new.png",
            "new-public-id",
            null,
            "now_showing");

        Assert.False(result.Succeeded);
        Assert.Equal("Movie poster could not be deleted. Please try again later.", result.ErrorMessage);
        Assert.Equal("https://example.com/old.png", movie.PosterURL);
        Assert.Equal("old-public-id", movie.PosterPublicId);
    }

    [Fact]
    public async Task DeleteMovieRejectsActiveOrUpcomingShowtimes()
    {
        var repository = new MovieRepositoryFake
        {
            Movies = { CreateMovie(1) },
            HasActiveOrUpcomingShowtimes = true
        };
        var service = CreateService(repository);

        var result = await service.DeleteMovieAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("Movie has active or upcoming showtimes", result.ErrorMessage);
        Assert.Single(repository.Movies);
    }

    [Fact]
    public async Task DeleteMovieHardDeletesWhenAllowed()
    {
        var movie = CreateMovie(1);
        movie.PosterPublicId = "poster-public-id";
        var repository = new MovieRepositoryFake
        {
            Movies = { movie }
        };
        var storage = new ImageStorageServiceFake();
        var service = CreateService(repository, storage);

        var result = await service.DeleteMovieAsync(1);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(repository.Movies);
        Assert.Contains("poster-public-id", storage.DeletedPublicIds);
    }

    [Fact]
    public async Task DeleteMovieFailsBeforeDatabaseDeleteWhenPosterDeletionFails()
    {
        var movie = CreateMovie(1);
        movie.PosterPublicId = "poster-public-id";
        var repository = new MovieRepositoryFake
        {
            Movies = { movie }
        };
        var storage = new ImageStorageServiceFake
        {
            DeleteFailures =
            {
                ["poster-public-id"] = new InvalidOperationException("Cloudinary delete failed")
            }
        };
        var service = CreateService(repository, storage);

        var result = await service.DeleteMovieAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("Movie poster could not be deleted. Please try again later.", result.ErrorMessage);
        Assert.Single(repository.Movies);
        Assert.Contains("poster-public-id", storage.DeletedPublicIds);
    }

    private static MovieService CreateService(
        MovieRepositoryFake repository,
        ImageStorageServiceFake? storage = null)
    {
        return new MovieService(repository, storage ?? new ImageStorageServiceFake());
    }

    private static MovieEntity CreateMovie(int movieId)
    {
        return new MovieEntity
        {
            MovieID = movieId,
            Title = "Avatar",
            AgeRating = "P",
            Director = "James Cameron",
            DurationMin = 120,
            Status = "coming_soon"
        };
    }

    private sealed class MovieRepositoryFake : IMovieRepository
    {
        public List<MovieEntity> Movies { get; init; } = [];

        public bool HasActiveOrUpcomingShowtimes { get; init; }

        public bool ReturnNullOnUpdate { get; init; }

        public IReadOnlyCollection<string> LastGenreNames { get; private set; } = [];

        public Task<MovieEntity?> GetByIdAsync(
            int movieId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Movies.FirstOrDefault(m => m.MovieID == movieId));
        }

        public Task<List<MovieEntity>> GetMoviesAsync(
            string? status,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MovieEntity?> GetMovieByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<MovieEntity>> SearchMoviesAsync(
            string keyword,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MovieEntity> AddMovieAsync(
            MovieEntity movie,
            IReadOnlyCollection<string> genreNames,
            CancellationToken cancellationToken = default)
        {
            movie.MovieID = Movies.Count == 0 ? 1 : Movies.Max(m => m.MovieID) + 1;
            LastGenreNames = genreNames;
            Movies.Add(movie);

            return Task.FromResult(movie);
        }

        public Task<MovieEntity?> UpdateMovieAsync(
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
            if (ReturnNullOnUpdate)
            {
                return Task.FromResult<MovieEntity?>(null);
            }

            var movie = Movies.FirstOrDefault(m => m.MovieID == movieId);
            if (movie is null)
            {
                return Task.FromResult<MovieEntity?>(null);
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
            LastGenreNames = genreNames ?? [];

            return Task.FromResult<MovieEntity?>(movie);
        }

        public Task<MovieEntity?> UpdatePosterAsync(
            int movieId,
            string posterUrl,
            string posterPublicId,
            DateTime updatedAt,
            CancellationToken cancellationToken = default)
        {
            var movie = Movies.FirstOrDefault(m => m.MovieID == movieId);
            if (movie is null)
                return Task.FromResult<MovieEntity?>(null);

            movie.PosterURL = posterUrl;
            movie.PosterPublicId = posterPublicId;
            movie.UpdatedAt = updatedAt;
            return Task.FromResult<MovieEntity?>(movie);
        }

        public Task<bool> HasActiveOrUpcomingShowtimesAsync(
            int movieId,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HasActiveOrUpcomingShowtimes);
        }

        public Task<bool> DeleteMovieAsync(
            int movieId,
            CancellationToken cancellationToken = default)
        {
            var movie = Movies.FirstOrDefault(m => m.MovieID == movieId);
            if (movie is null)
            {
                return Task.FromResult(false);
            }

            Movies.Remove(movie);

            return Task.FromResult(true);
        }
    }

    private sealed class ImageStorageServiceFake : IImageStorageService
    {
        public StoredImageResult UploadResult { get; init; } =
            new("https://example.com/poster.png", "poster-public-id");

        public Exception? UploadFailure { get; init; }

        public Dictionary<string, Exception> DeleteFailures { get; init; } = [];

        public string? UploadedFileName { get; private set; }

        public List<string> DeletedPublicIds { get; } = [];

        public Task<StoredImageResult> UploadImageAsync(
            Stream imageStream,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default)
        {
            UploadedFileName = fileName;

            if (UploadFailure is not null)
            {
                throw UploadFailure;
            }

            return Task.FromResult(UploadResult);
        }

        public Task DeleteImageAsync(
            string publicId,
            CancellationToken cancellationToken = default)
        {
            DeletedPublicIds.Add(publicId);

            if (DeleteFailures.TryGetValue(publicId, out var exception))
            {
                throw exception;
            }

            return Task.CompletedTask;
        }
    }
}
