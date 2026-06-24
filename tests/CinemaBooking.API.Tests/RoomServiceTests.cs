using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Rooms;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class RoomServiceTests
{
    [Fact]
    public async Task CreateRoomMapsRequestEnumsToDatabaseValues()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1 }
        };
        var service = new RoomService(repository);

        var result = await service.CreateRoomAsync(
            1,
            " Room 1 ",
            "THREE_D",
            100,
            "ACTIVE",
            " Main room ");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Room);
        Assert.Equal("Room 1", result.Room.RoomName);
        Assert.Equal("3D", result.Room.RoomType);
        Assert.Equal("active", result.Room.Status);
        Assert.Equal("Main room", result.Room.Description);
    }

    [Fact]
    public async Task CreateRoomRejectsInvalidCapacity()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1 }
        };
        var service = new RoomService(repository);

        var result = await service.CreateRoomAsync(
            1,
            "Room 1",
            "STANDARD",
            0,
            "ACTIVE",
            null);

        Assert.False(result.Succeeded);
        Assert.Equal("Capacity must be greater than 0", result.ErrorMessage);
        Assert.Null(result.Room);
        Assert.Empty(repository.Rooms);
    }

    [Fact]
    public async Task CreateRoomRejectsDuplicateNameWithinSameCinema()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1 },
            Rooms = { CreateRoom(1, 1, "Room 1") }
        };
        var service = new RoomService(repository);

        var result = await service.CreateRoomAsync(
            1,
            "Room 1",
            "STANDARD",
            100,
            "ACTIVE",
            null);

        Assert.False(result.Succeeded);
        Assert.Equal("Room name must be unique within the cinema", result.ErrorMessage);
        Assert.Null(result.Room);
    }

    [Fact]
    public async Task CreateRoomAllowsSameNameInDifferentCinema()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1, 2 },
            Rooms = { CreateRoom(1, 1, "Room 1") }
        };
        var service = new RoomService(repository);

        var result = await service.CreateRoomAsync(
            2,
            "Room 1",
            "VIP",
            50,
            "MAINTENANCE",
            null);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Room);
        Assert.Equal(2, result.Room.CinemaID);
        Assert.Equal("VIP", result.Room.RoomType);
        Assert.Equal("maintenance", result.Room.Status);
    }

    [Fact]
    public async Task UpdateRoomRejectsMissingRoom()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1 }
        };
        var service = new RoomService(repository);

        var result = await service.UpdateRoomAsync(
            404,
            1,
            "Room 1",
            "STANDARD",
            100,
            "ACTIVE",
            null);

        Assert.False(result.Succeeded);
        Assert.Equal("Room not found", result.ErrorMessage);
        Assert.Null(result.Room);
    }

    [Fact]
    public async Task UpdateRoomRejectsCapacityBelowExistingSeatCount()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1 },
            Rooms = { CreateRoom(1, 1, "Room 1") },
            SeatCounts = { [1] = 50 }
        };
        var service = new RoomService(repository);

        var result = await service.UpdateRoomAsync(
            1, 1, "Room 1", "STANDARD", 49, "ACTIVE", null);

        Assert.False(result.Succeeded);
        Assert.Equal("Capacity cannot be less than the current seat count (50)", result.ErrorMessage);
        Assert.Equal(100, repository.Rooms[0].Capacity);
    }

    [Fact]
    public async Task DeleteRoomRejectsActiveOrUpcomingSchedules()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1 },
            Rooms = { CreateRoom(1, 1, "Room 1") },
            RoomIdsWithActiveOrUpcomingShowtimes = { 1 }
        };
        var service = new RoomService(repository);

        var result = await service.DeleteRoomAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("Room has active or upcoming schedules", result.ErrorMessage);
        Assert.Single(repository.Rooms);
    }

    [Fact]
    public async Task DeleteRoomHardDeletesWhenAllowed()
    {
        var repository = new RoomRepositoryFake
        {
            CinemaIds = { 1 },
            Rooms = { CreateRoom(1, 1, "Room 1") }
        };
        var service = new RoomService(repository);

        var result = await service.DeleteRoomAsync(1);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(repository.Rooms);
    }

    private static Room CreateRoom(int roomId, int cinemaId, string roomName)
    {
        return new Room
        {
            RoomID = roomId,
            CinemaID = cinemaId,
            RoomName = roomName,
            RoomType = "Standard",
            Capacity = 100,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class RoomRepositoryFake : IRoomRepository
    {
        public HashSet<int> CinemaIds { get; init; } = [];

        public List<Room> Rooms { get; init; } = [];

        public HashSet<int> RoomIdsWithActiveOrUpcomingShowtimes { get; init; } = [];
        public Dictionary<int, int> SeatCounts { get; init; } = [];

        public Task<List<Room>> GetRoomsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Rooms);
        }

        public Task<Room?> GetByIdAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Rooms.FirstOrDefault(r => r.RoomID == roomId));
        }

        public Task<bool> CinemaExistsAsync(
            int cinemaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CinemaIds.Contains(cinemaId));
        }

        public Task<bool> NameExistsInCinemaAsync(
            int cinemaId,
            string roomName,
            int? excludingRoomId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Rooms.Any(r =>
                r.CinemaID == cinemaId
                && r.RoomName == roomName
                && (!excludingRoomId.HasValue || r.RoomID != excludingRoomId.Value)));
        }

        public Task<int> CountSeatsAsync(
            int roomId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SeatCounts.GetValueOrDefault(roomId));

        public Task<Room> AddAsync(
            Room room,
            CancellationToken cancellationToken = default)
        {
            room.RoomID = Rooms.Count == 0 ? 1 : Rooms.Max(r => r.RoomID) + 1;
            Rooms.Add(room);

            return Task.FromResult(room);
        }

        public Task<Room?> UpdateAsync(
            int roomId,
            int cinemaId,
            string roomName,
            string roomType,
            int capacity,
            string status,
            string? description,
            CancellationToken cancellationToken = default)
        {
            var room = Rooms.FirstOrDefault(r => r.RoomID == roomId);
            if (room is null)
            {
                return Task.FromResult<Room?>(null);
            }

            room.CinemaID = cinemaId;
            room.RoomName = roomName;
            room.RoomType = roomType;
            room.Capacity = capacity;
            room.Status = status;
            room.Description = description;

            return Task.FromResult<Room?>(room);
        }

        public Task<bool> HasActiveOrUpcomingShowtimesAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RoomIdsWithActiveOrUpcomingShowtimes.Contains(roomId));
        }

        public Task<bool> DeleteAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            var room = Rooms.FirstOrDefault(r => r.RoomID == roomId);
            if (room is null)
            {
                return Task.FromResult(false);
            }

            Rooms.Remove(room);

            return Task.FromResult(true);
        }
    }
}
