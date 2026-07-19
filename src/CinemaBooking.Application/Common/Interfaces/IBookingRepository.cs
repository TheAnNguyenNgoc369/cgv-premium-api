using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IBookingRepository
{
    Task<Showtime?> GetShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken = default);

    Task<(int TotalSeats, int BookedSeats)> GetShowtimeOccupancyAsync(
        int showtimeId,
        CancellationToken cancellationToken = default);

    Task<List<Seat>> GetSeatsByIdsAsync(
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetUnavailableSeatIdsAsync(
        int showtimeId,
        List<int> seatIds,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddSeatHoldsAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default);

    Task<List<SeatHold>> GetMyActiveHoldsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<List<SeatHold>> GetMyActiveHoldsForUpdateAsync(
        int userId,
        int showtimeId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task ReleaseSeatHoldsAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default);

    Task AddBookingAsync(
        Booking booking,
        CancellationToken cancellationToken = default);

    Task MarkHoldsAsConfirmedAsync(
        IEnumerable<SeatHold> seatHolds,
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByQRCodeAsync(
        string qrCode,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByCodeAsync(
        string bookingCode,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingWithFullDetailsForCheckInAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<int?> GetStaffCinemaIdAsync(
        int staffId,
        CancellationToken cancellationToken = default);

    Task<(List<Booking> Bookings, int TotalCount)> GetCheckInHistoryAsync(
        int? staffId,
        int? cinemaId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(List<Booking> Bookings, int TotalCount)> GetFnBPickupHistoryAsync(
        int? staffId,
        int? cinemaId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task UpdateBookingQRCodeAsync(
        int bookingId,
        string qrCode,
        CancellationToken cancellationToken = default);

    Task<List<Booking>> GetBookingsByUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetProductsByIdsAsync(
        List<int> productIds,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetAvailableProductsAsync(
        CancellationToken cancellationToken = default);

    Task<Voucher?> GetVoucherByCodeAsync(
        string voucherCode,
        CancellationToken cancellationToken = default);

    Task<Voucher?> GetVoucherByCodeWithLockAsync(
        string voucherCode,
        CancellationToken cancellationToken = default);

    Task IncrementVoucherUsageAsync(
        int voucherId,
        CancellationToken cancellationToken = default);

    Task ExtendBookingHoldsAsync(
        int bookingId,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveBookingHoldsAsync(
        int bookingId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task UpdateBookingStatusAsync(
        int bookingId,
        string status,
        CancellationToken cancellationToken = default);

    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task<bool> UpdateBookingFnBPickupAsync(
        string bookingCode,
        int staffId,
        CancellationToken cancellationToken = default);
}
