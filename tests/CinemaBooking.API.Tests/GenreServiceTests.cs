using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Genres;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class GenreServiceTests
{
    [Fact]
    public async Task CreateGenreTrimsName()
    {
        var repository = new GenreRepositoryFake();
        var service = new GenreService(repository);

        var result = await service.CreateGenreAsync(" Action ");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Genre);
        Assert.Equal("Action", result.Genre.GenreName);
    }

    [Fact]
    public async Task CreateGenreRejectsEmptyName()
    {
        var repository = new GenreRepositoryFake();
        var service = new GenreService(repository);

        var result = await service.CreateGenreAsync(" ");

        Assert.False(result.Succeeded);
        Assert.Equal("GenreName is required", result.ErrorMessage);
        Assert.Null(result.Genre);
        Assert.Empty(repository.Genres);
    }

    [Fact]
    public async Task CreateGenreRejectsDuplicateName()
    {
        var repository = new GenreRepositoryFake
        {
            Genres = { CreateGenre(1, "Action") }
        };
        var service = new GenreService(repository);

        var result = await service.CreateGenreAsync("Action");

        Assert.False(result.Succeeded);
        Assert.Equal("GenreName must be unique", result.ErrorMessage);
        Assert.Null(result.Genre);
    }

    [Fact]
    public async Task UpdateGenreRejectsMissingGenre()
    {
        var repository = new GenreRepositoryFake();
        var service = new GenreService(repository);

        var result = await service.UpdateGenreAsync(404, "Action");

        Assert.False(result.Succeeded);
        Assert.Equal("Genre not found", result.ErrorMessage);
        Assert.Null(result.Genre);
    }

    [Fact]
    public async Task UpdateGenreRejectsDuplicateName()
    {
        var repository = new GenreRepositoryFake
        {
            Genres =
            {
                CreateGenre(1, "Action"),
                CreateGenre(2, "Drama")
            }
        };
        var service = new GenreService(repository);

        var result = await service.UpdateGenreAsync(2, "Action");

        Assert.False(result.Succeeded);
        Assert.Equal("GenreName must be unique", result.ErrorMessage);
        Assert.Equal("Drama", repository.Genres[1].GenreName);
    }

    [Fact]
    public async Task DeleteGenreRejectsAssignedGenre()
    {
        var repository = new GenreRepositoryFake
        {
            Genres = { CreateGenre(1, "Action") },
            AssignedGenreIds = { 1 }
        };
        var service = new GenreService(repository);

        var result = await service.DeleteGenreAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("Genre is assigned to a movie", result.ErrorMessage);
        Assert.Single(repository.Genres);
    }

    [Fact]
    public async Task DeleteGenreHardDeletesUnusedGenre()
    {
        var repository = new GenreRepositoryFake
        {
            Genres = { CreateGenre(1, "Action") }
        };
        var service = new GenreService(repository);

        var result = await service.DeleteGenreAsync(1);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(repository.Genres);
    }

    private static Genre CreateGenre(int genreId, string genreName)
    {
        return new Genre
        {
            GenreID = genreId,
            GenreName = genreName
        };
    }

    private sealed class GenreRepositoryFake : IGenreRepository
    {
        public List<Genre> Genres { get; init; } = [];

        public HashSet<int> AssignedGenreIds { get; init; } = [];

        public Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Genres);
        }

        public Task<Genre?> GetByIdAsync(
            int genreId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Genres.FirstOrDefault(g => g.GenreID == genreId));
        }

        public Task<bool> NameExistsAsync(
            string genreName,
            int? excludingGenreId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Genres.Any(g =>
                g.GenreName == genreName
                && (!excludingGenreId.HasValue || g.GenreID != excludingGenreId.Value)));
        }

        public Task<Genre> AddAsync(
            Genre genre,
            CancellationToken cancellationToken = default)
        {
            genre.GenreID = Genres.Count == 0 ? 1 : Genres.Max(g => g.GenreID) + 1;
            Genres.Add(genre);

            return Task.FromResult(genre);
        }

        public Task<Genre?> UpdateAsync(
            int genreId,
            string genreName,
            CancellationToken cancellationToken = default)
        {
            var genre = Genres.FirstOrDefault(g => g.GenreID == genreId);
            if (genre is null)
            {
                return Task.FromResult<Genre?>(null);
            }

            genre.GenreName = genreName;

            return Task.FromResult<Genre?>(genre);
        }

        public Task<bool> IsAssignedToAnyMovieAsync(
            int genreId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AssignedGenreIds.Contains(genreId));
        }

        public Task<bool> DeleteAsync(
            int genreId,
            CancellationToken cancellationToken = default)
        {
            var genre = Genres.FirstOrDefault(g => g.GenreID == genreId);
            if (genre is null)
            {
                return Task.FromResult(false);
            }

            Genres.Remove(genre);

            return Task.FromResult(true);
        }
    }
}
