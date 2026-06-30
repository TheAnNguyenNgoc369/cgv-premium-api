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
            1, 1, DateTime.UtcNow.AddDays(1), 100_000);

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
            1, 1, DateTime.UtcNow.AddDays(1), 100_000);

        Assert.True(result.Succeeded);
        Assert.Equal("scheduled", result.Showtime!.Status);
        Assert.Equal(1, repository.AddCallCount);
    }

    [Fact]
    public async Task CreateShowtimeAsync_StartTimeHasPassed_CreatesCompletedShowtime()
    {
        var repository = new StubShowtimeRepository
        {
            ActiveSeats = [new Seat { SeatID = 1, RoomID = 1, Status = "active" }]
        };
        var service = new ShowtimeService(repository);

        var result = await service.CreateShowtimeAsync(
            1, 1, DateTime.UtcNow.AddMinutes(-1), 100_000);

        Assert.True(result.Succeeded);
        Assert.Equal("completed", result.Showtime!.Status);
    }

    [Fact]
    public async Task UpdateShowtimeAsync_ActiveBookingAndScheduleChange_ReturnsConflict()
    {
        var startTime = DateTime.UtcNow.AddDays(1);
        var repository = new StubShowtimeRepository
        {
            HasActiveBookingOrHold = true,
            ExistingShowtime = new Showtime
            {
                ShowtimeID = 1, MovieID = 1, RoomID = 1,
                StartTime = startTime, BasePrice = 100_000, Status = "scheduled"
            }
        };
        var service = new ShowtimeService(repository);

        var result = await service.UpdateShowtimeAsync(
            1, 1, 1, startTime.AddHours(1), 100_000, null);

        Assert.False(result.Succeeded);
        Assert.Equal("Showtime has active bookings or seat holds", result.ErrorMessage);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task UpdateShowtimeAsync_ActiveBookingAndCancellation_CancelsWithoutChangingDetails()
    {
        var startTime = DateTime.UtcNow.AddDays(1);
        var repository = new StubShowtimeRepository
        {
            HasActiveBookingOrHold = true,
            ExistingShowtime = new Showtime
            {
                ShowtimeID = 1, MovieID = 1, RoomID = 1,
                StartTime = startTime, BasePrice = 100_000, Status = "scheduled"
            }
        };
        var service = new ShowtimeService(repository);

        var result = await service.UpdateShowtimeAsync(
            1, 1, 1, startTime, 100_000, "CANCELLED");

        Assert.True(result.Succeeded);
        Assert.Equal("cancelled", result.Showtime!.Status);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task UpdateShowtimeAsync_ManualCompletedStatus_UsesProvidedStatus()
    {
        var startTime = DateTime.UtcNow.AddDays(1);
        var repository = new StubShowtimeRepository
        {
            ExistingShowtime = new Showtime
            {
                ShowtimeID = 1, MovieID = 1, RoomID = 1,
                StartTime = startTime, BasePrice = 100_000, Status = "scheduled"
            }
        };
        var service = new ShowtimeService(repository);

        var result = await service.UpdateShowtimeAsync(
            1, 1, 1, startTime, 100_000, "COMPLETED");

        Assert.True(result.Succeeded);
        Assert.Equal("completed", result.Showtime!.Status);
    }

    [Fact]
    public async Task UpdateShowtimeAsync_CancelledWithoutStatus_RemainsCancelled()
    {
        var startTime = DateTime.UtcNow.AddDays(1);
        var repository = new StubShowtimeRepository
        {
            ExistingShowtime = new Showtime
            {
                ShowtimeID = 1, MovieID = 1, RoomID = 1,
                StartTime = startTime, BasePrice = 100_000, Status = "cancelled"
            }
        };
        var service = new ShowtimeService(repository);

        var result = await service.UpdateShowtimeAsync(
            1, 1, 1, startTime, 100_000, null);

        Assert.True(result.Succeeded);
        Assert.Equal("cancelled", result.Showtime!.Status);
    }

    [Fact]
    public async Task UpdateShowtimeAsync_CancelledWithoutBookings_CanReturnToScheduled()
    {
        var startTime = DateTime.UtcNow.AddDays(1);
        var repository = new StubShowtimeRepository
        {
            ExistingShowtime = new Showtime
            {
                ShowtimeID = 1, MovieID = 1, RoomID = 1,
                StartTime = startTime, BasePrice = 100_000, Status = "cancelled"
            }
        };
        var service = new ShowtimeService(repository);

        var result = await service.UpdateShowtimeAsync(
            1, 1, 1, startTime, 100_000, "SCHEDULED");

        Assert.True(result.Succeeded);
        Assert.Equal("scheduled", result.Showtime!.Status);
    }

    [Fact]
    public async Task UpdateShowtimeAsync_ActiveBookingAndNonCancelledStatus_ReturnsConflict()
    {
        var startTime = DateTime.UtcNow.AddDays(1);
        var repository = new StubShowtimeRepository
        {
            HasActiveBookingOrHold = true,
            ExistingShowtime = new Showtime
            {
                ShowtimeID = 1, MovieID = 1, RoomID = 1,
                StartTime = startTime, BasePrice = 100_000, Status = "scheduled"
            }
        };
        var service = new ShowtimeService(repository);

        var result = await service.UpdateShowtimeAsync(
            1, 1, 1, startTime, 100_000, "COMPLETED");

        Assert.False(result.Succeeded);
        Assert.Equal("Showtime has active bookings or seat holds", result.ErrorMessage);
    }

    private sealed class StubShowtimeRepository : IShowtimeRepository
    {
        public List<Seat> ActiveSeats { get; init; } = [];
        public Showtime? ExistingShowtime { get; init; }
        public bool HasActiveBookingOrHold { get; init; }
        public int AddCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

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
            CancellationToken cancellationToken = default) => Task.FromResult(HasActiveBookingOrHold);
        public Task<bool> IsSoldOutAsync(int showtimeId, int capacity, DateTime now,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            return Task.FromResult<Showtime?>(showtime);
        }
        public Task<bool> DeleteAsync(int showtimeId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Showtime?> GetShowtimeByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingShowtime?.ShowtimeID == id ? ExistingShowtime : null);
        public Task<List<int>> GetBookedSeatIdsAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<int>());
        public Task<List<int>> GetHeldSeatIdsAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<int>());
    }
}
