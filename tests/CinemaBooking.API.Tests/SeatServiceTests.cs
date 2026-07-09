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
            Room = new Room { RoomID = 1, Capacity = 0, Status = "inactive" },
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
            Room = new Room { RoomID = 1, Capacity = 0, Status = "inactive" }
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
            Room = new Room { RoomID = 1, CinemaID = 1, Status = "inactive" },
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

    [Fact]
    public async Task BulkUpdate_WhenSeatsHaveRelations_UpdatesCurrentSeats()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, CinemaID = 1, Status = "inactive" },
            SelectedSeats =
            [
                new Seat { SeatID = 1, RoomID = 1, SeatTypeID = 1, Status = "active" },
                new Seat { SeatID = 2, RoomID = 1, SeatTypeID = 1, Status = "active" }
            ],
            RelatedSeatIds = [2]
        };
        var service = new SeatService(repository);

        var result = await service.BulkUpdateAsync(
            1, [new SeatSelector("ids", ["1", "2"])], null, "INACTIVE", null);

        Assert.True(result.Succeeded);
        Assert.Equal([1, 2], repository.UpdatedSeatIds);
    }

    [Fact]
    public async Task GenerateSeats_AboveMaximumRows_ReturnsValidationErrorWithoutWriting()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "inactive" },
            SeatType = new SeatType { SeatTypeID = 1 }
        };
        var service = new SeatService(repository);

        var result = await service.GenerateSeatsAsync(1, 101, 1, 1, "ACTIVE");

        Assert.False(result.Succeeded);
        Assert.Equal("Rows must not exceed 100", result.ErrorMessage);
        Assert.Equal(0, repository.ReplaceLayoutCallCount);
    }

    [Fact]
    public async Task GenerateSeats_WhenRoomAlreadyHasSeats_ReplacesCurrentLayout()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "inactive" },
            SeatType = new SeatType { SeatTypeID = 1 },
            ExistingSeatCount = 5
        };
        var service = new SeatService(repository);

        var result = await service.GenerateSeatsAsync(1, 2, 3, 1, "ACTIVE");

        Assert.True(result.Succeeded);
        Assert.Equal(6, result.Result!.Seats.Count);
        Assert.Equal(1, repository.ReplaceLayoutCallCount);
    }

    [Theory]
    [InlineData("DISABLED")]
    [InlineData("MAINTENANCE")]
    public async Task GenerateSeats_WithUnsupportedStatus_ReturnsActiveInactiveMessage(string status)
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "inactive" },
            SeatType = new SeatType { SeatTypeID = 1 }
        };
        var service = new SeatService(repository);

        var result = await service.GenerateSeatsAsync(1, 1, 1, 1, status);

        Assert.False(result.Succeeded);
        Assert.Equal("Status must be ACTIVE or INACTIVE", result.ErrorMessage);
        Assert.Equal(0, repository.ReplaceLayoutCallCount);
    }

    [Fact]
    public async Task BulkUpdate_ValidMutation_ExecutesInsideTransaction()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "inactive" },
            SelectedSeats =
            [new Seat { SeatID = 1, RoomID = 1, SeatTypeID = 1, Status = "active" }]
        };
        var unitOfWork = new RecordingUnitOfWork();
        var service = new SeatService(repository, unitOfWork);

        var result = await service.BulkUpdateAsync(
            1, [new SeatSelector("ids", ["1"])], null, "INACTIVE", null);

        Assert.True(result.Succeeded);
        Assert.Equal(1, unitOfWork.ExecutionCount);
    }

    [Fact]
    public async Task GenerateSeats_WhenRoomIsActive_ReturnsValidationError()
    {
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "active" },
            SeatType = new SeatType { SeatTypeID = 1 }
        };
        var service = new SeatService(repository);

        var result = await service.GenerateSeatsAsync(1, 1, 1, 1, "ACTIVE");

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Seats can only be managed when the room is INACTIVE",
            result.ErrorMessage);
        Assert.Equal(0, repository.ReplaceLayoutCallCount);
    }

    [Fact]
    public async Task BulkDelete_LowercaseRowSelector_SoftDeletesMatchingSeats()
    {
        var seat = new Seat
        {
            SeatID = 1,
            RoomID = 1,
            SeatRow = "B",
            SeatCol = 1,
            SeatTypeID = 1,
            Status = "active"
        };
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "inactive" },
            SelectedSeats = [seat]
        };
        var service = new SeatService(repository);

        var result = await service.BulkDeleteAsync(
            1, [new SeatSelector("rows", ["b"])]);

        Assert.True(result.Succeeded);
        Assert.Equal("inactive", seat.Status);
    }

    [Fact]
    public async Task BulkDelete_WhenAnyTargetDoesNotExist_DoesNotChangeAnySeat()
    {
        var seat = new Seat
        {
            SeatID = 1,
            RoomID = 1,
            SeatRow = "A",
            SeatCol = 1,
            SeatTypeID = 1,
            Status = "active"
        };
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "inactive" },
            SelectedSeats = [seat]
        };
        var unitOfWork = new RecordingUnitOfWork();
        var service = new SeatService(repository, unitOfWork);

        var result = await service.BulkDeleteAsync(
            1, [new SeatSelector("rows", ["a", "not_found"])]);

        Assert.False(result.Succeeded);
        Assert.Equal("active", seat.Status);
        Assert.Empty(repository.UpdatedSeatIds);
        Assert.Equal(0, unitOfWork.ExecutionCount);
    }

    [Fact]
    public async Task BulkDelete_WithoutSelectors_SoftDeletesAllCurrentSeats()
    {
        var seats = new List<Seat>
        {
            new() { SeatID = 1, RoomID = 1, SeatRow = "A", SeatCol = 1, SeatTypeID = 1, Status = "active" },
            new() { SeatID = 2, RoomID = 1, SeatRow = "B", SeatCol = 1, SeatTypeID = 1, Status = "active" }
        };
        var repository = new StubSeatRepository
        {
            Room = new Room { RoomID = 1, Status = "inactive" },
            SelectedSeats = seats
        };
        var service = new SeatService(repository);

        var result = await service.BulkDeleteAsync(1, []);

        Assert.True(result.Succeeded);
        Assert.All(seats, seat => Assert.Equal("inactive", seat.Status));
        Assert.Equal([1, 2], repository.UpdatedSeatIds);
    }

    private sealed class StubSeatRepository : ISeatRepository
    {
        public Room? Room { get; set; }
        public SeatType? SeatType { get; set; }
        public int AddCallCount { get; private set; }
        public int ReplaceLayoutCallCount { get; private set; }
        public int ExistingSeatCount { get; init; }
        public List<Seat> SelectedSeats { get; init; } = [];
        public HashSet<int> RelatedSeatIds { get; init; } = [];
        public List<int> UpdatedSeatIds { get; } = [];

        public Task<List<Seat>> GetSeatsByRoomAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SelectedSeats);
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
            return Task.FromResult(
                SelectedSeats.FirstOrDefault(
                    seat => seat.RoomID == roomId && seat.SeatID == seatId));
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
            return Task.FromResult(ExistingSeatCount);
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
            return Task.FromResult(RelatedSeatIds.Contains(seatId));
        }

        public Task<List<Seat>> GetSeatsBySelectorAsync(
            int roomId,
            IReadOnlyCollection<SeatSelector> selectors,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SelectedSeats);
        }

        public Task<Seat?> AddAsync(
            Seat seat,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            seat.SeatID = 1;
            seat.SeatType = SeatType!;
            return Task.FromResult<Seat?>(seat);
        }

        public Task<Seat?> UpdateAsync(
            int roomId,
            int seatId,
            int? seatTypeId,
            string status,
            bool isGap,
            CancellationToken cancellationToken = default)
        {
            UpdatedSeatIds.Add(seatId);
            var seat = SelectedSeats.First(item => item.SeatID == seatId);
            seat.SeatTypeID = seatTypeId;
            seat.Status = status;
            seat.IsGap = isGap;
            return Task.FromResult<Seat?>(seat);
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

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int ExecutionCount { get; private set; }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return await operation();
        }
    }
}
