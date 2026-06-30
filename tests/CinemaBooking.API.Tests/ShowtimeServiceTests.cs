using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Showtimes;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class ShowtimeServiceTests
{
    [Fact]
    public async Task CreateShowtimeAsync_RoomHasNoActiveSeats_ReturnsValidationError()
    {
        var repository = new StubShowtimeRepository { ActiveSeats = [] };
        var service = new ShowtimeService(repository);

        var result = await service.CreateShowtimeAsync(
            1, 1, DateTime.UtcNow.AddDays(1), 100_000, "scheduled");

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Room has no seats. Please configure seats before creating a showtime.",
            result.ErrorMessage);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task CreateShowtimeAsync_RoomHasActiveSeat_CreatesShowtime()
    {
        var repository = new StubShowtimeRepository
        {
            ActiveSeats = [new Seat { SeatID = 1, RoomID = 1, Status = "active" }]
        };
        var service = new ShowtimeService(repository);

        var result = await service.CreateShowtimeAsync(
            1, 1, DateTime.UtcNow.AddDays(1), 100_000, "scheduled");

        Assert.True(result.Succeeded);
        Assert.Equal(1, repository.AddCallCount);
    }

    private sealed class StubShowtimeRepository : IShowtimeRepository
    {
        public List<Seat> ActiveSeats { get; init; } = [];
        public int AddCallCount { get; private set; }

        public Task<Movie?> GetMovieAsync(int movieId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Movie?>(new Movie { MovieID = movieId, DurationMin = 120, Status = "now_showing" });
        public Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(new Room { RoomID = roomId, Status = "active" });
        public Task<List<Seat>> GetSeatsByRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveSeats);
        public Task<bool> HasConflictAsync(int roomId, DateTime startTime, DateTime endTime,
            int? excludingShowtimeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Showtime> AddAsync(Showtime showtime, CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            return Task.FromResult(showtime);
        }

        public Task<(List<Showtime> Items, int TotalItems)> GetShowtimesAsync(int? movieId, int? cinemaId,
            string? movieName, string? roomName, DateOnly? date, string? status, int page, int pageSize,
            string sortBy, bool descending, CancellationToken cancellationToken = default) =>
            Task.FromResult((new List<Showtime>(), 0));
        public Task<bool> HasActiveBookingOrHoldAsync(int showtimeId, DateTime now,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> IsSoldOutAsync(int showtimeId, int capacity, DateTime now,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken cancellationToken = default) =>
            Task.FromResult<Showtime?>(showtime);
        public Task<bool> DeleteAsync(int showtimeId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Showtime?> GetShowtimeByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Showtime?>(null);
        public Task<List<int>> GetBookedSeatIdsAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<int>());
        public Task<List<int>> GetHeldSeatIdsAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<int>());
    }
}
