using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Rooms;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class RoomServiceScopeTests
{
    [Fact]
    public async Task CreateRoom_ForAnotherCinema_ReturnsForbiddenMessage()
    {
        var repository = new StubRoomRepository();
        var service = new RoomService(repository);

        var result = await service.CreateRoomAsync(
            2, "Room 1", "STANDARD", "ACTIVE", null, managerCinemaId: 1);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Access denied. This resource belongs to another cinema branch outside your management scope.",
            result.ErrorMessage);
        Assert.Equal(0, repository.AddCallCount);
    }

    private sealed class StubRoomRepository : IRoomRepository
    {
        public int AddCallCount { get; private set; }
        public Task<List<Room>> GetRoomsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Room>());
        public Task<Room?> GetByIdAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(null);
        public Task<bool> CinemaExistsAsync(int cinemaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<bool> NameExistsInCinemaAsync(int cinemaId, string roomName,
            int? excludingRoomId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<int> CountSeatsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
        public Task<Room> AddAsync(Room room, CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            return Task.FromResult(room);
        }
        public Task<Room?> UpdateAsync(int roomId, int cinemaId, string roomName,
            string roomType, string status, string? description,
            CancellationToken cancellationToken = default) => Task.FromResult<Room?>(null);
        public Task<bool> HasActiveOrUpcomingShowtimesAsync(
            int roomId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> DeleteAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
