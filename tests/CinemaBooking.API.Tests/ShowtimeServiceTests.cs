using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Showtimes;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class ShowtimeServiceTests
{
    [Fact]
    public async Task CreateCalculatesEndTimeAndMapsStatus()
    {
        var repository = ValidRepository();
        var service = new ShowtimeService(repository);
        var start = new DateTime(2026, 7, 12, 19, 0, 0);

        var result = await service.CreateShowtimeAsync(1, 1, start, 120000, "SCHEDULED");

        Assert.True(result.Succeeded);
        Assert.Equal(start.AddMinutes(199), result.Showtime!.EndTime);
        Assert.Equal("scheduled", result.Showtime.Status);
    }

    [Fact]
    public async Task CreateRejectsRoomConflict()
    {
        var repository = ValidRepository();
        repository.HasConflict = true;
        var service = new ShowtimeService(repository);

        var result = await service.CreateShowtimeAsync(
            1, 1, new DateTime(2026, 7, 12, 19, 0, 0), 120000, "scheduled");

        Assert.False(result.Succeeded);
        Assert.Equal("Showtime conflicts with another showtime in the same room", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateRejectsActiveBookingOrHold()
    {
        var repository = ValidRepository();
        repository.Showtimes.Add(new Showtime { ShowtimeID = 1, Movie = repository.Movie, Room = repository.Room });
        repository.HasGuardRecord = true;
        var service = new ShowtimeService(repository);

        var result = await service.UpdateShowtimeAsync(
            1, 1, 1, new DateTime(2026, 7, 12, 19, 0, 0), 120000, "scheduled");

        Assert.False(result.Succeeded);
        Assert.Equal("Showtime has active bookings or seat holds", result.ErrorMessage);
    }

    [Fact]
    public async Task ListRejectsInvalidStatus()
    {
        var service = new ShowtimeService(ValidRepository());

        var result = await service.GetShowtimesAsync(null, null, null, "invalid", 1, 10, null, null);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Invalid showtime status. (scheduled, ongoing, completed, cancelled)",
            result.ErrorMessage);
    }

    private static ShowtimeRepositoryFake ValidRepository() => new()
    {
        Movie = new Movie { MovieID = 1, Title = "Interstellar", AgeRating = "C13", DurationMin = 169, Status = "now_showing" },
        Room = new Room { RoomID = 1, RoomName = "Hall 01", RoomType = "IMAX", Capacity = 200, Status = "active" }
    };

    private sealed class ShowtimeRepositoryFake : IShowtimeRepository
    {
        public Movie Movie { get; init; } = null!;
        public Room Room { get; init; } = null!;
        public List<Showtime> Showtimes { get; } = [];
        public bool HasConflict { get; set; }
        public bool HasGuardRecord { get; set; }

        public Task<(List<Showtime> Items, int TotalItems)> GetShowtimesAsync(string? movieName, string? roomName,
            DateOnly? date, string? status, int page, int pageSize, string sortBy, bool descending,
            CancellationToken cancellationToken = default) => Task.FromResult((Showtimes, Showtimes.Count));
        public Task<Movie?> GetMovieAsync(int movieId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Movie?>(movieId == Movie.MovieID ? Movie : null);
        public Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(roomId == Room.RoomID ? Room : null);
        public Task<bool> HasConflictAsync(int roomId, DateTime startTime, DateTime endTime,
            int? excludingShowtimeId = null, CancellationToken cancellationToken = default) => Task.FromResult(HasConflict);
        public Task<bool> HasActiveBookingOrHoldAsync(int showtimeId, DateTime now,
            CancellationToken cancellationToken = default) => Task.FromResult(HasGuardRecord);
        public Task<bool> IsSoldOutAsync(int showtimeId, int capacity, DateTime now,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Showtime> AddAsync(Showtime showtime, CancellationToken cancellationToken = default)
        {
            showtime.ShowtimeID = 1; showtime.Movie = Movie; showtime.Room = Room; Showtimes.Add(showtime);
            return Task.FromResult(showtime);
        }
        public Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken cancellationToken = default) =>
            Task.FromResult<Showtime?>(showtime);
        public Task<bool> DeleteAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Showtimes.RemoveAll(s => s.ShowtimeID == showtimeId) > 0);
        public Task<List<Showtime>> GetShowtimesByMovieAsync(int movieId, DateOnly? date, int? cinemaId,
            CancellationToken cancellationToken = default) => Task.FromResult(Showtimes);
        public Task<Showtime?> GetShowtimeByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Showtimes.FirstOrDefault(s => s.ShowtimeID == id));
        public Task<List<Seat>> GetSeatsByRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Seat>());
        public Task<List<int>> GetBookedSeatIdsAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<int>());
        public Task<List<int>> GetHeldSeatIdsAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<int>());
    }
}
