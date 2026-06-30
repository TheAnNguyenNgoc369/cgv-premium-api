using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Movie;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class MovieServiceTests
{
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

        public Task<List<Movie>> GetMoviesAsync(
            string? status, IReadOnlyCollection<int> genreIds,
            CancellationToken cancellationToken = default)
        {
            Status = status;
            GenreIds = genreIds;
            return Task.FromResult(new List<Movie>());
        }

        public Task<Movie?> GetByIdAsync(int movieId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Movie?>(null);
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
            CancellationToken cancellationToken = default) => Task.FromResult<Movie?>(null);
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
