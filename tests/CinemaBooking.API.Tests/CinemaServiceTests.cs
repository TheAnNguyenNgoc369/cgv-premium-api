using CinemaBooking.Application.Cinemas;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class CinemaServiceTests
{
    [Fact]
    public async Task CreateCinemaDefaultsStatusToActive()
    {
        var repository = new CinemaRepositoryFake();
        var service = new CinemaService(repository);

        var result = await service.CreateCinemaAsync(
            " CGV Vincom Dong Khoi ",
            " 72 Le Thanh Ton ",
            null);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Cinema);
        Assert.Equal("CGV Vincom Dong Khoi", result.Cinema.CinemaName);
        Assert.Equal("72 Le Thanh Ton", result.Cinema.Address);
        Assert.Equal("active", result.Cinema.Status);
        Assert.Equal(result.Cinema.CreatedAt, result.Cinema.UpdatedAt);
    }

    [Fact]
    public async Task CreateCinemaRejectsInvalidStatus()
    {
        var repository = new CinemaRepositoryFake();
        var service = new CinemaService(repository);

        var result = await service.CreateCinemaAsync(
            "CGV Vincom Dong Khoi",
            "72 Le Thanh Ton",
            "closed");

        Assert.False(result.Succeeded);
        Assert.Equal("Status must be active, inactive, or maintenance", result.ErrorMessage);
        Assert.Null(result.Cinema);
        Assert.Empty(repository.Cinemas);
    }

    [Fact]
    public async Task UpdateCinemaRejectsMissingCinema()
    {
        var repository = new CinemaRepositoryFake();
        var service = new CinemaService(repository);

        var result = await service.UpdateCinemaAsync(
            404,
            "CGV Vincom Dong Khoi",
            "72 Le Thanh Ton",
            "active");

        Assert.False(result.Succeeded);
        Assert.Equal("Cinema not found", result.ErrorMessage);
        Assert.Null(result.Cinema);
    }

    [Fact]
    public async Task UpdateCinemaSetsUpdatedAtToCurrentTime()
    {
        var repository = new CinemaRepositoryFake
        {
            Cinemas =
            {
                CreateCinema(1, updatedAt: DateTime.UtcNow.AddDays(-1))
            }
        };
        var service = new CinemaService(repository);
        var beforeUpdate = DateTime.UtcNow;

        var result = await service.UpdateCinemaAsync(
            1,
            "CGV Vincom Dong Khoi",
            "72 Le Thanh Ton",
            "maintenance");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Cinema);
        Assert.Equal("maintenance", result.Cinema.Status);
        Assert.True(result.Cinema.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public async Task DeleteCinemaRejectsCinemaWithRooms()
    {
        var repository = new CinemaRepositoryFake
        {
            Cinemas = { CreateCinema(1) },
            CinemaIdsWithRooms = { 1 }
        };
        var service = new CinemaService(repository);

        var result = await service.DeleteCinemaAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("Cinema has rooms", result.ErrorMessage);
        Assert.Equal("active", repository.Cinemas[0].Status);
    }

    [Fact]
    public async Task DeleteCinemaRejectsCinemaWithAssignedUsers()
    {
        var repository = new CinemaRepositoryFake
        {
            Cinemas = { CreateCinema(1) },
            CinemaIdsWithAssignedUsers = { 1 }
        };
        var service = new CinemaService(repository);

        var result = await service.DeleteCinemaAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("Cinema has assigned users", result.ErrorMessage);
        Assert.Equal("active", repository.Cinemas[0].Status);
    }

    [Fact]
    public async Task DeleteCinemaSoftDeletesCinema()
    {
        var repository = new CinemaRepositoryFake
        {
            Cinemas = { CreateCinema(1) }
        };
        var service = new CinemaService(repository);
        var beforeDelete = DateTime.UtcNow;

        var result = await service.DeleteCinemaAsync(1);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("inactive", repository.Cinemas[0].Status);
        Assert.True(repository.Cinemas[0].UpdatedAt >= beforeDelete);
    }

    private static Cinema CreateCinema(
        int cinemaId,
        string status = "active",
        DateTime? updatedAt = null)
    {
        var createdAt = DateTime.UtcNow.AddDays(-2);

        return new Cinema
        {
            CinemaID = cinemaId,
            CinemaName = "CGV Vincom Dong Khoi",
            Address = "72 Le Thanh Ton",
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt ?? createdAt
        };
    }

    private sealed class CinemaRepositoryFake : ICinemaRepository
    {
        public List<Cinema> Cinemas { get; init; } = [];

        public HashSet<int> CinemaIdsWithRooms { get; init; } = [];

        public HashSet<int> CinemaIdsWithAssignedUsers { get; init; } = [];

        public Task<List<Cinema>> GetCinemasAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Cinemas);
        }

        public Task<List<Cinema>> GetActiveCinemasAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Cinemas.Where(c => c.Status == "active").ToList());
        }

        public Task<Cinema?> GetByIdAsync(
            int cinemaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Cinemas.FirstOrDefault(c => c.CinemaID == cinemaId));
        }

        public Task<Cinema> AddAsync(
            Cinema cinema,
            CancellationToken cancellationToken = default)
        {
            cinema.CinemaID = Cinemas.Count == 0 ? 1 : Cinemas.Max(c => c.CinemaID) + 1;
            Cinemas.Add(cinema);

            return Task.FromResult(cinema);
        }

        public Task<Cinema?> UpdateAsync(
            int cinemaId,
            string cinemaName,
            string address,
            string status,
            DateTime updatedAt,
            CancellationToken cancellationToken = default)
        {
            var cinema = Cinemas.FirstOrDefault(c => c.CinemaID == cinemaId);
            if (cinema is null)
            {
                return Task.FromResult<Cinema?>(null);
            }

            cinema.CinemaName = cinemaName;
            cinema.Address = address;
            cinema.Status = status;
            cinema.UpdatedAt = updatedAt;

            return Task.FromResult<Cinema?>(cinema);
        }

        public Task<bool> HasRoomsAsync(
            int cinemaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CinemaIdsWithRooms.Contains(cinemaId));
        }

        public Task<bool> HasAssignedUsersAsync(
            int cinemaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CinemaIdsWithAssignedUsers.Contains(cinemaId));
        }

        public Task<Cinema?> SoftDeleteAsync(
            int cinemaId,
            DateTime updatedAt,
            CancellationToken cancellationToken = default)
        {
            var cinema = Cinemas.FirstOrDefault(c => c.CinemaID == cinemaId);
            if (cinema is null)
            {
                return Task.FromResult<Cinema?>(null);
            }

            cinema.Status = "inactive";
            cinema.UpdatedAt = updatedAt;

            return Task.FromResult<Cinema?>(cinema);
        }
    }
}
