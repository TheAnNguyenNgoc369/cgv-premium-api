using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.SeatTypes;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class SeatTypeServiceTests
{
    [Fact]
    public async Task CreateSeatType_ValidRequest_CreatesNormalizedSeatType()
    {
        var repository = new StubSeatTypeRepository();
        var service = new SeatTypeService(repository);

        var result = await service.CreateSeatTypeAsync(" Premium ", 2, 30000);

        Assert.True(result.Succeeded);
        Assert.Equal("premium", result.SeatType?.TypeName);
        Assert.Equal(2, result.SeatType?.Capacity);
        Assert.Equal(30000, result.SeatType?.ExtraPrice);
    }

    [Fact]
    public async Task DeleteSeatType_WhenUsedBySeat_ReturnsFailure()
    {
        var repository = new StubSeatTypeRepository
        {
            ExistingSeatType = new SeatType { SeatTypeID = 1, TypeName = "standard" },
            IsUsedBySeat = true
        };
        var service = new SeatTypeService(repository);

        var result = await service.DeleteSeatTypeAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("Seat type is currently used by one or more seats.", result.ErrorMessage);
        Assert.False(repository.DeleteCalled);
    }

    private sealed class StubSeatTypeRepository : ISeatTypeRepository
    {
        public SeatType? ExistingSeatType { get; set; }
        public bool IsUsedBySeat { get; set; }
        public bool DeleteCalled { get; private set; }

        public Task<List<SeatType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<SeatType>());
        }

        public Task<SeatType?> GetByIdAsync(
            int seatTypeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingSeatType);
        }

        public Task<bool> NameExistsAsync(
            string typeName,
            int? excludingSeatTypeId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<SeatType> AddAsync(
            SeatType seatType,
            CancellationToken cancellationToken = default)
        {
            seatType.SeatTypeID = 4;
            return Task.FromResult(seatType);
        }

        public Task<SeatType?> UpdateAsync(
            int seatTypeId,
            string typeName,
            int capacity,
            decimal extraPrice,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> IsUsedByAnySeatAsync(
            int seatTypeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsUsedBySeat);
        }

        public Task<bool> DeleteAsync(
            int seatTypeId,
            CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.FromResult(true);
        }
    }
}
