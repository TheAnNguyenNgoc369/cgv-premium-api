using CinemaBooking.Application.Bookings;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class BookingServiceSeatHoldTests
{
    [Fact]
    public async Task HoldSeatsUsesUtcAndTenMinuteExpiration()
    {
        var repository = new BookingRepositoryFake { Showtime = CreateShowtime() };
        var before = DateTime.UtcNow;
        var service = new BookingService(repository, null!);

        var result = await service.HoldSeatsAsync(1, 10, [100]);

        var hold = Assert.Single(repository.Holds);
        Assert.True(result.Succeeded);
        Assert.InRange(hold.HeldAt, before, DateTime.UtcNow);
        Assert.Equal(TimeSpan.FromMinutes(10), hold.ExpiresAt - hold.HeldAt);
    }

    [Fact]
    public async Task HoldSeatsReturnsConflictWhenConcurrentInsertLosesRace()
    {
        var repository = new BookingRepositoryFake
        {
            Showtime = CreateShowtime(),
            CanAddHolds = false
        };
        var service = new BookingService(repository, null!);

        var result = await service.HoldSeatsAsync(1, 10, [100]);

        Assert.False(result.Succeeded);
        Assert.Equal("One or more seats are already booked or being held", result.ErrorMessage);
    }

    private static Showtime CreateShowtime() => new()
    {
        ShowtimeID = 10,
        Room = new Room { Cinema = new Cinema { Status = "active" } }
    };

    private sealed class BookingRepositoryFake : IBookingRepository
    {
        public Showtime? Showtime { get; init; }
        public bool CanAddHolds { get; init; } = true;
        public List<SeatHold> Holds { get; } = [];
        public Task<Showtime?> GetShowtimeAsync(int showtimeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Showtime);
        public Task<List<int>> GetUnavailableSeatIdsAsync(int showtimeId, List<int> seatIds, int currentUserId,
            CancellationToken cancellationToken = default) => Task.FromResult(new List<int>());
        public Task<bool> TryAddSeatHoldsAsync(IEnumerable<SeatHold> seatHolds,
            CancellationToken cancellationToken = default)
        {
            Holds.AddRange(seatHolds);
            return Task.FromResult(CanAddHolds);
        }
        public Task<List<Seat>> GetSeatsByIdsAsync(List<int> seatIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<SeatHold>> GetMyActiveHoldsAsync(int userId, int showtimeId, List<int> seatIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddBookingAsync(Booking booking, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkHoldsAsConfirmedAsync(IEnumerable<SeatHold> seatHolds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Booking?> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Booking>> GetBookingsByUserAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Product>> GetProductsByIdsAsync(List<int> productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Voucher?> GetVoucherByCodeAsync(string voucherCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task IncrementVoucherUsageAsync(int voucherId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateBookingStatusAsync(int bookingId, string status, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
