using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Notifications;

public sealed class NotificationOutbox : INotificationOutbox
{
    private readonly CinemaBookingDbContext _db;
    public NotificationOutbox(CinemaBookingDbContext db) => _db = db;
    public Task EnqueueBookingSuccessAsync(int bookingId, CancellationToken ct = default) => EnqueueAsync(
        $"BookingSuccess:{bookingId}", "BookingSuccess", bookingId, null, null, null, ct);
    public Task EnqueueRefundCompletedAsync(int bookingId, decimal amount, DateTime completedAt, CancellationToken ct = default) => EnqueueAsync(
        $"RefundCompleted:{bookingId}", "RefundCompleted", bookingId, amount, completedAt, null, ct);
    public Task EnqueueWalletRefundAsync(int userId, decimal amount, DateTime occurredAt, CancellationToken ct = default) => EnqueueAsync(
        $"WalletRefund:{userId}:{occurredAt:yyyyMMddHHmmssfff}", "WalletRefund", userId, amount, occurredAt, null, ct);
    public Task EnqueueWalletPaymentAsync(int userId, decimal amount, DateTime occurredAt, CancellationToken ct = default) => EnqueueAsync(
        $"WalletPayment:{userId}:{occurredAt:yyyyMMddHHmmssfff}", "WalletPayment", userId, amount, occurredAt, null, ct);

    // Manager/Admin notifications
    public Task EnqueueRevenueAnomalyAsync(int cinemaId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"RevenueAnomaly:{cinemaId}:{DateTime.UtcNow:yyyyMMddHHmmss}", "RevenueAnomaly", cinemaId, null, null, message, ct);
    public Task EnqueueDailySummaryAsync(int? cinemaId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"DailySummary:{cinemaId ?? 0}:{DateTime.UtcNow:yyyyMMdd}", "DailySummary", cinemaId ?? 0, null, null, message, ct);
    public Task EnqueueTopSellingMovieAsync(int movieId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"TopSellingMovie:{movieId}:{DateTime.UtcNow:yyyyMMdd}", "TopSellingMovie", movieId, null, null, message, ct);
    public Task EnqueueLowPerformingMovieAsync(int movieId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"LowPerformingMovie:{movieId}:{DateTime.UtcNow:yyyyMMdd}", "LowPerformingMovie", movieId, null, null, message, ct);
    public Task EnqueueMovieEndingSoonAsync(int movieId, string message, int daysRemaining, CancellationToken ct = default) => EnqueueAsync(
        $"MovieEndingSoon:{movieId}:{DateTime.UtcNow:yyyyMMdd}", "MovieEndingSoon", movieId, null, null, message, ct);
    public Task EnqueueShowtimeSoldOutAsync(int showtimeId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"ShowtimeSoldOut:{showtimeId}", "ShowtimeSoldOut", showtimeId, null, null, message, ct);
    public Task EnqueueLowOccupancyShowtimeAsync(int showtimeId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"LowOccupancyShowtime:{showtimeId}:{DateTime.UtcNow:yyyyMMdd}", "LowOccupancyShowtime", showtimeId, null, null, message, ct);
    public Task EnqueueVoucherExpiringSoonAsync(int voucherId, string message, int daysRemaining, CancellationToken ct = default) => EnqueueAsync(
        $"VoucherExpiringSoon:{voucherId}:{daysRemaining}d", "VoucherExpiringSoon", voucherId, null, null, message, ct);
    public Task EnqueueVoucherOutOfStockAsync(int voucherId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"VoucherOutOfStock:{voucherId}", "VoucherOutOfStock", voucherId, null, null, message, ct);
    public Task EnqueueNewCustomerSummaryAsync(string message, CancellationToken ct = default) => EnqueueAsync(
        $"NewCustomerSummary:{DateTime.UtcNow:yyyyMMdd}", "NewCustomerSummary", 0, null, null, message, ct);
    public Task EnqueuePaymentIssueAsync(string message, string? paymentMethod = null, CancellationToken ct = default) => EnqueueAsync(
        $"PaymentIssue:{paymentMethod}:{DateTime.UtcNow:yyyyMMddHHmm}", "PaymentIssue", 0, null, null, message, ct);
    public Task EnqueueRoomCreatedAsync(int roomId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"RoomCreated:{roomId}", "RoomCreated", roomId, null, null, message, ct);
    public Task EnqueueRoomUpdatedAsync(int roomId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"RoomUpdated:{roomId}", "RoomUpdated", roomId, null, null, message, ct);
    public Task EnqueueRoomInactiveAsync(int roomId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"RoomInactive:{roomId}", "RoomInactive", roomId, null, null, message, ct);
    public Task EnqueueShowtimeStartingSoonAsync(int showtimeId, string message, int minutesRemaining, CancellationToken ct = default) => EnqueueAsync(
        $"ShowtimeStartingSoon:{showtimeId}:{minutesRemaining}m", "ShowtimeStartingSoon", showtimeId, null, null, message, ct);
    public Task EnqueueShowtimeCreatedAsync(int showtimeId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"ShowtimeCreated:{showtimeId}", "ShowtimeCreated", showtimeId, null, null, message, ct);
    public Task EnqueueShowtimeUpdatedAsync(int showtimeId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"ShowtimeUpdated:{showtimeId}:{DateTime.UtcNow:yyyyMMddHHmmss}", "ShowtimeUpdated", showtimeId, null, null, message, ct);
    public Task EnqueueShowtimeDeletedAsync(int showtimeId, string message, CancellationToken ct = default) => EnqueueAsync(
        $"ShowtimeDeleted:{showtimeId}:{DateTime.UtcNow:yyyyMMddHHmmss}", "ShowtimeDeleted", showtimeId, null, null, message, ct);

    private async Task EnqueueAsync(string eventId, string eventType, int referenceId, decimal? amount, DateTime? occurredAt, string? message, CancellationToken ct)
    {
        if (await _db.NotificationOutbox.AnyAsync(x => x.EventId == eventId, ct)) return;
        _db.NotificationOutbox.Add(new Domain.Entities.NotificationOutbox { EventId = eventId, EventType = eventType,
            ReferenceId = referenceId, Amount = amount, OccurredAt = occurredAt, Message = message, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }
}
