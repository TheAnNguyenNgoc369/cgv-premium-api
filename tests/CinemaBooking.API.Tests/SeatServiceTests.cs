using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Seats;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class SeatServiceTests
{
    [Fact]
    public async Task CreateSeatMapsRequestEnumsToDatabaseValues()
    {
        var repository = new SeatRepositoryFake
        {
            Rooms = { CreateRoom(1, capacity: 10) }
        };
        var service = new SeatService(repository);

        var result = await service.CreateSeatAsync(
            1,
            " a ",
            1,
            "A1",
            "VIP",
            "MAINTENANCE");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Seat);
        Assert.Equal("A", result.Seat.SeatRow);
        Assert.Equal(1, result.Seat.SeatCol);
        Assert.Equal(2, result.Seat.SeatTypeID);
        Assert.Equal("inactive", result.Seat.Status);
    }

    [Fact]
    public async Task CreateSeatRejectsCapacityOverflow()
    {
        var repository = new SeatRepositoryFake
        {
            Rooms = { CreateRoom(1, capacity: 1) },
            Seats = { CreateSeat(1, 1, "A", 1) }
        };
        var service = new SeatService(repository);

        var result = await service.CreateSeatAsync(
            1,
            "A",
            2,
            null,
            "STANDARD",
            "ACTIVE");

        Assert.False(result.Succeeded);
        Assert.Equal("Room capacity exceeded", result.ErrorMessage);
        Assert.Single(repository.Seats);
    }

    [Fact]
    public async Task UpdateSeatRejectsActiveOrUpcomingSchedules()
    {
        var repository = new SeatRepositoryFake
        {
            Rooms = { CreateRoom(1, capacity: 10) },
            Seats = { CreateSeat(1, 1, "A", 1) },
            RoomIdsWithActiveOrUpcomingShowtimes = { 1 }
        };
        var service = new SeatService(repository);

        var result = await service.UpdateSeatAsync(
            1,
            1,
            "COUPLE",
            "ACTIVE");

        Assert.False(result.Succeeded);
        Assert.Equal("Room has active or upcoming schedules", result.ErrorMessage);
        Assert.Equal(1, repository.Seats[0].SeatTypeID);
    }

    [Fact]
    public async Task DeleteSeatRejectsRelatedBookingOrHoldRecords()
    {
        var repository = new SeatRepositoryFake
        {
            Rooms = { CreateRoom(1, capacity: 10) },
            Seats = { CreateSeat(1, 1, "A", 1) },
            RelatedSeatIds = { 1 }
        };
        var service = new SeatService(repository);

        var result = await service.DeleteSeatAsync(1, 1);

        Assert.False(result.Succeeded);
        Assert.Equal("Seat has related booking or hold records", result.ErrorMessage);
        Assert.Single(repository.Seats);
    }

    [Fact]
    public async Task ReplaceLayoutUpdatesGridAndPreservesRelatedObsoleteSeatsAsInactive()
    {
        var repository = new SeatRepositoryFake
        {
            Rooms = { CreateRoom(1, capacity: 10) },
            Seats =
            {
                CreateSeat(1, 1, "A", 1),
                CreateSeat(2, 1, "A", 2),
                CreateSeat(3, 1, "B", 1)
            },
            RelatedSeatIds = { 3 }
        };
        var service = new SeatService(repository);

        var result = await service.ReplaceLayoutAsync(
            1,
            totalRows: 1,
            seatsPerRow: 1,
            seatType: "COUPLE",
            seatStatus: "ACTIVE");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, repository.Seats.Count);

        var activeSeat = repository.Seats.Single(seat => seat.SeatRow == "A" && seat.SeatCol == 1);
        Assert.Equal(3, activeSeat.SeatTypeID);
        Assert.Equal("active", activeSeat.Status);

        var preservedSeat = repository.Seats.Single(seat => seat.SeatID == 3);
        Assert.Equal("inactive", preservedSeat.Status);
    }

    [Fact]
    public async Task ReplaceLayoutRejectsCapacityOverflow()
    {
        var repository = new SeatRepositoryFake
        {
            Rooms = { CreateRoom(1, capacity: 3) }
        };
        var service = new SeatService(repository);

        var result = await service.ReplaceLayoutAsync(
            1,
            totalRows: 2,
            seatsPerRow: 2,
            seatType: "STANDARD",
            seatStatus: "ACTIVE");

        Assert.False(result.Succeeded);
        Assert.Equal("Room capacity exceeded", result.ErrorMessage);
        Assert.Empty(repository.Seats);
    }

    private static Room CreateRoom(int roomId, int capacity)
    {
        return new Room
        {
            RoomID = roomId,
            CinemaID = 1,
            RoomName = "Room 1",
            RoomType = "Standard",
            Capacity = capacity,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Seat CreateSeat(int seatId, int roomId, string rowLabel, int seatNumber)
    {
        return new Seat
        {
            SeatID = seatId,
            RoomID = roomId,
            SeatRow = rowLabel,
            SeatCol = seatNumber,
            SeatTypeID = 1,
            SeatType = new SeatType { SeatTypeID = 1, TypeName = "standard" },
            Status = "active"
        };
    }

    private sealed class SeatRepositoryFake : ISeatRepository
    {
        public List<Room> Rooms { get; init; } = [];

        public List<Seat> Seats { get; init; } = [];

        public HashSet<int> RoomIdsWithActiveOrUpcomingShowtimes { get; init; } = [];

        public HashSet<int> RelatedSeatIds { get; init; } = [];

        private static readonly List<SeatType> SeatTypes =
        [
            new SeatType { SeatTypeID = 1, TypeName = "standard" },
            new SeatType { SeatTypeID = 2, TypeName = "vip" },
            new SeatType { SeatTypeID = 3, TypeName = "couple" }
        ];

        public Task<List<Seat>> GetSeatsByRoomAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Seats
                .Where(seat => seat.RoomID == roomId)
                .OrderBy(seat => seat.SeatRow)
                .ThenBy(seat => seat.SeatCol)
                .ToList());
        }

        public Task<Room?> GetRoomByIdAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Rooms.FirstOrDefault(room => room.RoomID == roomId));
        }

        public Task<Seat?> GetSeatByIdAsync(
            int roomId,
            int seatId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Seats.FirstOrDefault(seat =>
                seat.RoomID == roomId && seat.SeatID == seatId));
        }

        public Task<SeatType?> GetSeatTypeByNameAsync(
            string typeName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SeatTypes.FirstOrDefault(type => type.TypeName == typeName));
        }

        public Task<int> CountSeatsByRoomAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Seats.Count(seat => seat.RoomID == roomId));
        }

        public Task<bool> SeatPositionExistsAsync(
            int roomId,
            string rowLabel,
            int seatNumber,
            int? excludingSeatId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Seats.Any(seat =>
                seat.RoomID == roomId
                && seat.SeatRow == rowLabel
                && seat.SeatCol == seatNumber
                && (!excludingSeatId.HasValue || seat.SeatID != excludingSeatId.Value)));
        }

        public Task<bool> HasActiveOrUpcomingShowtimesAsync(
            int roomId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RoomIdsWithActiveOrUpcomingShowtimes.Contains(roomId));
        }

        public Task<bool> HasSeatRelationsAsync(
            int seatId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RelatedSeatIds.Contains(seatId));
        }

        public Task<Seat> AddAsync(
            Seat seat,
            CancellationToken cancellationToken = default)
        {
            seat.SeatID = Seats.Count == 0 ? 1 : Seats.Max(s => s.SeatID) + 1;
            seat.SeatType = SeatTypes.Single(type => type.SeatTypeID == seat.SeatTypeID);
            Seats.Add(seat);

            return Task.FromResult(seat);
        }

        public Task<Seat?> UpdateAsync(
            int roomId,
            int seatId,
            int seatTypeId,
            string status,
            CancellationToken cancellationToken = default)
        {
            var seat = Seats.FirstOrDefault(s => s.RoomID == roomId && s.SeatID == seatId);
            if (seat is null)
            {
                return Task.FromResult<Seat?>(null);
            }

            seat.SeatTypeID = seatTypeId;
            seat.SeatType = SeatTypes.Single(type => type.SeatTypeID == seatTypeId);
            seat.Status = status;

            return Task.FromResult<Seat?>(seat);
        }

        public Task<bool> DeleteAsync(
            int roomId,
            int seatId,
            CancellationToken cancellationToken = default)
        {
            var seat = Seats.FirstOrDefault(s => s.RoomID == roomId && s.SeatID == seatId);
            if (seat is null)
            {
                return Task.FromResult(false);
            }

            Seats.Remove(seat);

            return Task.FromResult(true);
        }

        public Task<List<Seat>> ReplaceLayoutAsync(
            int roomId,
            IReadOnlyCollection<Seat> seats,
            CancellationToken cancellationToken = default)
        {
            var requestedSeats = seats.ToDictionary(seat => (seat.SeatRow, seat.SeatCol), seat => seat);

            foreach (var existingSeat in Seats.Where(seat => seat.RoomID == roomId).ToList())
            {
                if (requestedSeats.Remove((existingSeat.SeatRow, existingSeat.SeatCol), out var requestedSeat))
                {
                    existingSeat.SeatTypeID = requestedSeat.SeatTypeID;
                    existingSeat.SeatType = SeatTypes.Single(type => type.SeatTypeID == requestedSeat.SeatTypeID);
                    existingSeat.Status = requestedSeat.Status;
                    continue;
                }

                if (RelatedSeatIds.Contains(existingSeat.SeatID))
                {
                    existingSeat.Status = "inactive";
                    continue;
                }

                Seats.Remove(existingSeat);
            }

            foreach (var seat in requestedSeats.Values)
            {
                seat.SeatID = Seats.Count == 0 ? 1 : Seats.Max(s => s.SeatID) + 1;
                seat.SeatType = SeatTypes.Single(type => type.SeatTypeID == seat.SeatTypeID);
                Seats.Add(seat);
            }

            return GetSeatsByRoomAsync(roomId, cancellationToken);
        }
    }
}
