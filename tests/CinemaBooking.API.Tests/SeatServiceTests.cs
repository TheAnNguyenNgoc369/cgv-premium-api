using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Seats;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class SeatServiceTests
{
    [Fact]
    public async Task CreateSeat_WhenRoomCapacityIsZero_CreatesSeat()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Capacity = 0 },
            SeatType = new SeatType { SeatTypeID = 1, TypeName = "standard", Capacity = 1 }
        };
        var service = new SeatService(repository);

        var result = await service.CreateSeatAsync(1, "A", 1, 1, "ACTIVE", false);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Seat);
        Assert.Equal(1, repository.AddCallCount);
    }

    [Fact]
    public async Task ReplaceLayout_WhenSeatTypeIsMissing_ReturnsFailure()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Capacity = 0 }
        };
        var service = new SeatService(repository);

        var result = await service.ReplaceLayoutAsync(
            1,
            1,
            1,
            [new SeatLayoutSeatItem("A", 1, "A1", 99, "ACTIVE", false)]);

        Assert.False(result.Succeeded);
        Assert.Equal("Seat type not found", result.ErrorMessage);
        Assert.Equal(0, repository.ReplaceLayoutCallCount);
    }

    [Fact]
    public async Task CreateSeat_WhenRoomBelongsToAnotherCinema_ReturnsForbiddenMessage()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, CinemaID = 1 },
            SeatType = new SeatType { SeatTypeID = 1, TypeName = "standard", Capacity = 1 }
        };
        var service = new SeatService(repository);

        var result = await service.CreateSeatAsync(
            1, "A", 1, 1, "ACTIVE", false, managerCinemaId: 2);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Access denied. This resource belongs to another cinema branch outside your management scope.",
            result.ErrorMessage);
        Assert.Equal(0, repository.AddCallCount);
    }

    private sealed class StubSeatRepository : ISeatRepository
    {
        public Room? Room { get; set; }
        public SeatType? SeatType { get; set; }
        public int AddCallCount { get; private set; }
        public int ReplaceLayoutCallCount { get; private set; }

        public Task<List<Seat>> GetSeatsByRoomAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<Seat>());
        }

        public Task<Room?> GetRoomByIdAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Room);
        }

        public Task<Seat?> GetSeatByIdAsync(
            int roomId,
            int seatId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Seat?>(null);
        }

        public Task<Seat?> GetSeatByIdAsync(
            int seatId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Seat?>(null);
        }

        public Task<SeatType?> GetSeatTypeByNameAsync(
            string typeName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SeatType);
        }

        public Task<SeatType?> GetSeatTypeByIdAsync(
            int seatTypeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                SeatType?.SeatTypeID == seatTypeId
                    ? SeatType
                    : null);
        }

        public Task<int> CountSeatsByRoomAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<bool> SeatPositionExistsAsync(
            int roomId,
            string rowLabel,
            int seatNumber,
            int? excludingSeatId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasActiveOrUpcomingShowtimesAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasSeatRelationsAsync(
            int seatId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<List<Seat>> GetSeatsBySelectorAsync(
            int roomId,
            SeatSelector selector,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<Seat>());
        }

        public Task<Seat> AddAsync(
            Seat seat,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            seat.SeatID = 1;
            seat.SeatType = SeatType!;
            return Task.FromResult(seat);
        }

        public Task<Seat?> UpdateAsync(
            int roomId,
            int seatId,
            int? seatTypeId,
            string status,
            bool isGap,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(
            int seatId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<List<Seat>> ReplaceLayoutAsync(
            int roomId,
            IReadOnlyCollection<Seat> seats,
            CancellationToken cancellationToken = default)
        {
            ReplaceLayoutCallCount++;
            return Task.FromResult(seats.ToList());
        }
    }
}
