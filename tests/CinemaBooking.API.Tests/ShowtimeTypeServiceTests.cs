using CinemaBooking.Application.ShowtimeTypes;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class ShowtimeTypeServiceTests
{
    [Fact]
    public async Task PreviewAsync_ComingSoonMovieWithRoomOverlap_ReturnsRoomOverlap()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var repository = new StubShowtimeTypeRepository
        {
            HasConflict = true,
            Movie = new Movie
            {
                MovieID = 1,
                Title = "Coming soon",
                DurationMin = 90,
                Status = "coming_soon",
                ShowingFrom = date.AddDays(1)
            }
        };
        var service = new ShowtimeTypeService(repository);

        var result = await service.PreviewAsync(1, 1, date, date, 1, 100000, null, CancellationToken.None);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Items);
        Assert.True(item.IsConflict);
        Assert.Equal("ROOM_OVERLAP", item.ConflictCode);
    }

    private sealed class StubShowtimeTypeRepository : IShowtimeTypeRepository
    {
        public Movie Movie { get; set; } = new()
        {
            MovieID = 1,
            Title = "Movie",
            DurationMin = 90,
            Status = "now_showing"
        };

        public bool HasConflict { get; set; }

        public Task<ShowtimeTypePage> ListAsync(int? cinemaId, bool? isActive, int page, int pageSize, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<ShowtimeType?> GetAsync(int id, bool tracking, CancellationToken ct) =>
            Task.FromResult<ShowtimeType?>(new ShowtimeType
            {
                ShowtimeTypeID = id,
                CinemaID = 1,
                Name = "Default",
                IsActive = true,
                Slots = [new ShowtimeTypeSlot { StartTime = TimeSpan.FromHours(9) }]
            });

        public Task<bool> CinemaExistsAsync(int id, CancellationToken ct) => Task.FromResult(true);

        public Task<bool> NameExistsAsync(int cinemaId, string name, int? exceptId, CancellationToken ct) =>
            Task.FromResult(false);

        public Task<Movie?> GetMovieAsync(int id, CancellationToken ct) => Task.FromResult<Movie?>(Movie);

        public Task<Room?> GetRoomAsync(int id, CancellationToken ct) =>
            Task.FromResult<Room?>(new Room
            {
                RoomID = id,
                CinemaID = 1,
                RoomName = "Room 1",
                Status = "active",
                Cinema = new Cinema
                {
                    CinemaID = 1,
                    CinemaName = "Cinema",
                    Address = "Address",
                    Status = "active"
                }
            });

        public Task<bool> HasValidSeatAsync(int roomId, CancellationToken ct) => Task.FromResult(true);

        public Task<bool> HasConflictAsync(int roomId, DateTime start, DateTime end, CancellationToken ct) =>
            Task.FromResult(HasConflict);

        public Task AddAsync(ShowtimeType value, CancellationToken ct) => throw new NotImplementedException();

        public void ReplaceSlots(ShowtimeType value, IEnumerable<TimeSpan> slots) => throw new NotImplementedException();

        public Task SaveAsync(CancellationToken ct) => throw new NotImplementedException();

        public Task AddShowtimesAsync(IEnumerable<Showtime> values, CancellationToken ct) => Task.CompletedTask;

        public Task<T> TransactionAsync<T>(Func<Task<T>> action, CancellationToken ct) => action();

        public Task LockScheduleAsync(int roomId, int cinemaId, CancellationToken ct) => Task.CompletedTask;
    }
}
