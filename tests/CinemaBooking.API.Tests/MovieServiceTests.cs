using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Movie;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class MovieServiceTests
{
    [Theory]
    [InlineData(1, 2, "coming_soon")]
    [InlineData(-1, 1, "now_showing")]
    [InlineData(-2, -1, "ended")]
    public async Task CreateMovieAsync_Dates_CalculatesStatus(
        int startOffsetDays, int endOffsetDays, string expectedStatus)
    {
        var repository = new StubMovieRepository();
        var service = new MovieService(repository, new StubImageStorageService());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await service.CreateMovieAsync(
            "Movie", null, "P", "Director", null, null, 120,
            today.AddDays(startOffsetDays), today.AddDays(endOffsetDays),
            null, null, null);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus, result.Movie!.Status);
    }

    [Fact]
    public async Task UpdateMovieAsync_StatusIsNull_PreservesExistingStatus()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var repository = new StubMovieRepository
        {
            ExistingMovie = new Movie
            {
                MovieID = 1, Status = "ended", PosterPublicId = null
            }
        };
        var service = new MovieService(repository, new StubImageStorageService());

        var result = await service.UpdateMovieAsync(
            1, "Movie", null, "P", "Director", null, null, 120,
            today, today.AddDays(1), null, null, null, null);

        Assert.True(result.Succeeded);
        Assert.Equal("ended", repository.UpdatedStatus);
    }

    [Fact]
    public async Task UpdateMovieAsync_ManualStatus_UsesProvidedStatus()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var repository = new StubMovieRepository
        {
            ExistingMovie = new Movie
            {
                MovieID = 1, Status = "ended", PosterPublicId = null
            }
        };
        var service = new MovieService(repository, new StubImageStorageService());

        var result = await service.UpdateMovieAsync(
            1, "Movie", null, "P", "Director", null, null, 120,
            today, today.AddDays(1), null, null, null, "NOW_SHOWING");

        Assert.True(result.Succeeded);
        Assert.Equal("now_showing", repository.UpdatedStatus);
    }

    [Fact]
    public async Task GetMoviesAsync_GenreIds_ForwardsOrFilterWithStatus()
    {
        var repository = new StubMovieRepository();
        var service = new MovieService(repository, new StubImageStorageService());

        await service.GetMoviesAsync("now_showing", new[] { 1, 2 });

        Assert.Equal("now_showing", repository.Status);
        Assert.Equal(new[] { 1, 2 }, repository.GenreIds);
    }

    private sealed class StubMovieRepository : IMovieRepository
    {
        public string? Status { get; private set; }
        public IReadOnlyCollection<int> GenreIds { get; private set; } = Array.Empty<int>();
        public Movie? ExistingMovie { get; init; }
        public string? UpdatedStatus { get; private set; }

        public Task<List<Movie>> GetMoviesAsync(
            string? status, IReadOnlyCollection<int> genreIds,
            CancellationToken cancellationToken = default)
        {
            Status = status;
            GenreIds = genreIds;
            return Task.FromResult(new List<Movie>());
        }

        public Task<Movie?> GetByIdAsync(int movieId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingMovie?.MovieID == movieId ? ExistingMovie : null);
        public Task<Movie?> GetMovieByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Movie?>(null);
        public Task<List<Movie>> SearchMoviesAsync(string keyword, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Movie>());
        public Task<Movie> AddMovieAsync(Movie movie, IReadOnlyCollection<string> genreNames,
            CancellationToken cancellationToken = default) => Task.FromResult(movie);
        public Task<Movie?> UpdateMovieAsync(int movieId, string title,
            IReadOnlyCollection<string>? genreNames, string ageRating, string director, string? cast,
            string? synopsis, int durationMinutes, DateOnly? showingFromDate, DateOnly? showingToDate,
            string? posterUrl, string? posterPublicId, string? trailerUrl, string status, DateTime updatedAt,
            CancellationToken cancellationToken = default)
        {
            UpdatedStatus = status;
            if (ExistingMovie is not null) ExistingMovie.Status = status;
            return Task.FromResult(ExistingMovie);
        }
        public Task<Movie?> UpdatePosterAsync(int movieId, string posterUrl, string posterPublicId,
            DateTime updatedAt, CancellationToken cancellationToken = default) => Task.FromResult<Movie?>(null);
        public Task<bool> HasActiveOrUpcomingShowtimesAsync(int movieId, DateTime now,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> DeleteMovieAsync(int movieId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StubImageStorageService : IImageStorageService
    {
        public Task<StoredImageResult> UploadImageAsync(Stream imageStream, string fileName, string folder,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
