using CinemaBooking.Application.Bookings;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class BookingServiceCinemaStatusTests
{
    [Fact]
    public async Task HoldSeatsRejectsInactiveCinema()
    {
        var repository = new BookingRepositoryFake
        {
            Showtime = CreateShowtime("inactive")
        };
        var service = new BookingService(repository, null!);

        var result = await service.HoldSeatsAsync(1, 10, [100]);

        Assert.False(result.Succeeded);
        Assert.Equal("Rạp không hoạt động", result.ErrorMessage);
        Assert.Empty(repository.AddedSeatHolds);
    }

    [Fact]
    public async Task CreateBookingRejectsMaintenanceCinema()
    {
        var repository = new BookingRepositoryFake
        {
            Showtime = CreateShowtime("maintenance")
        };
        var service = new BookingService(repository, null!);

        var result = await service.CreateBookingAsync(1, 10, [100], [], null);

        Assert.False(result.Succeeded);
        Assert.Equal("Rạp không hoạt động", result.ErrorMessage);
        Assert.Null(result.Booking);
        Assert.Null(repository.AddedBooking);
    }

    private static Showtime CreateShowtime(string cinemaStatus)
    {
        return new Showtime
        {
            ShowtimeID = 10,
            RoomID = 20,
            Room = new Room
            {
                RoomID = 20,
                CinemaID = 30,
                Cinema = new Cinema
                {
                    CinemaID = 30,
                    CinemaName = "CGV Vincom Dong Khoi",
                    Address = "72 Le Thanh Ton",
                    Status = cinemaStatus
                }
            }
        };
    }

    private sealed class BookingRepositoryFake : IBookingRepository
    {
        public Showtime? Showtime { get; init; }

        public List<SeatHold> AddedSeatHolds { get; } = [];

        public Booking? AddedBooking { get; private set; }

        public Task<Showtime?> GetShowtimeAsync(
            int showtimeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Showtime?.ShowtimeID == showtimeId ? Showtime : null);
        }

        public Task<bool> TryAddSeatHoldsAsync(
            IEnumerable<SeatHold> seatHolds,
            CancellationToken cancellationToken = default)
        {
            AddedSeatHolds.AddRange(seatHolds);
            return Task.FromResult(true);
        }

        public Task AddBookingAsync(
            Booking booking,
            CancellationToken cancellationToken = default)
        {
            AddedBooking = booking;

            return Task.CompletedTask;
        }

        public Task<List<Seat>> GetSeatsByIdsAsync(
            List<int> seatIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<int>> GetUnavailableSeatIdsAsync(
            int showtimeId,
            List<int> seatIds,
            int currentUserId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<SeatHold>> GetMyActiveHoldsAsync(
            int userId,
            int showtimeId,
            List<int> seatIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task MarkHoldsAsConfirmedAsync(
            IEnumerable<SeatHold> seatHolds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Booking?> GetBookingByIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<Booking>> GetBookingsByUserAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<Product>> GetProductsByIdsAsync(
            List<int> productIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Voucher?> GetVoucherByCodeAsync(
            string voucherCode,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task IncrementVoucherUsageAsync(
            int voucherId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateBookingStatusAsync(
            int bookingId,
            string status,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
