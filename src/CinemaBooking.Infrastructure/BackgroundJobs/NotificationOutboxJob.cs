using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Email;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class NotificationOutboxJob(IServiceScopeFactory scopeFactory, ILogger<NotificationOutboxJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Notification outbox batch failed. The job will retry on the next interval.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IBookingEmailService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;
        var processingLeaseExpiresAt = now.AddMinutes(5);
        var candidateIds = await db.NotificationOutbox
            .Where(x =>
                (x.Status == "pending" || x.Status == "processing")
                && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.NotificationOutboxID)
            .Take(20)
            .ToListAsync(ct);

        var claimedIds = new List<long>();
        foreach (var eventId in candidateIds)
        {
            var affectedRows = await db.NotificationOutbox
                .Where(x => x.NotificationOutboxID == eventId
                    && (x.Status == "pending" || x.Status == "processing")
                    && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, "processing")
                        .SetProperty(x => x.NextAttemptAt, processingLeaseExpiresAt),
                    ct);

            if (affectedRows == 1)
                claimedIds.Add(eventId);
        }

        var events = await db.NotificationOutbox
            .Where(x => claimedIds.Contains(x.NotificationOutboxID))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
        foreach (var item in events)
        {
            try
            {
                if (item.EventType == "BookingSuccess") await ProcessBookingAsync(db, email, item, ct);
                else if (item.EventType == "RefundCompleted") await ProcessRefundAsync(db, email, item, ct);
                else if (item.EventType == "RevenueAnomaly") await ProcessRevenueAnomalyAsync(db, notificationService, item, ct);
                else if (item.EventType == "DailySummary") await ProcessDailySummaryAsync(db, notificationService, item, ct);
                else if (item.EventType == "TopSellingMovie") await ProcessTopSellingMovieAsync(db, notificationService, item, ct);
                else if (item.EventType == "LowPerformingMovie") await ProcessLowPerformingMovieAsync(db, notificationService, item, ct);
                else if (item.EventType == "MovieEndingSoon") await ProcessMovieEndingSoonAsync(db, notificationService, item, ct);
                else if (item.EventType == "ShowtimeSoldOut") await ProcessShowtimeSoldOutAsync(db, notificationService, item, ct);
                else if (item.EventType == "LowOccupancyShowtime") await ProcessLowOccupancyShowtimeAsync(db, notificationService, item, ct);
                else if (item.EventType == "VoucherExpiringSoon") await ProcessVoucherExpiringSoonAsync(db, notificationService, item, ct);
                else if (item.EventType == "VoucherOutOfStock") await ProcessVoucherOutOfStockAsync(db, notificationService, item, ct);
                else if (item.EventType == "NewCustomerSummary") await ProcessNewCustomerSummaryAsync(db, notificationService, item, ct);
                else if (item.EventType == "PaymentIssue") await ProcessPaymentIssueAsync(db, notificationService, item, ct);
                else if (item.EventType == "RoomCreated") await ProcessRoomCreatedAsync(db, notificationService, item, ct);
                else if (item.EventType == "RoomUpdated") await ProcessRoomUpdatedAsync(db, notificationService, item, ct);
                else if (item.EventType == "RoomInactive") await ProcessRoomInactiveAsync(db, notificationService, item, ct);
                else if (item.EventType == "ShowtimeStartingSoon") await ProcessShowtimeStartingSoonAsync(db, notificationService, item, ct);
                else if (item.EventType == "ShowtimeCreated") await ProcessShowtimeCreatedAsync(db, notificationService, item, ct);
                else if (item.EventType == "ShowtimeUpdated") await ProcessShowtimeUpdatedAsync(db, notificationService, item, ct);
                else if (item.EventType == "ShowtimeDeleted") await ProcessShowtimeDeletedAsync(db, notificationService, item, ct);
                item.Status = "processed"; item.ProcessedAt = DateTime.UtcNow; item.LastError = null; item.NextAttemptAt = null;

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Notification outbox event {EventId} failed", item.EventId);

                try
                {
                    ScheduleRetry(item);
                    item.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception saveException) when (saveException is not OperationCanceledException)
                {
                    logger.LogError(
                        saveException,
                        "Failed to persist retry state for notification outbox event {EventId}",
                        item.EventId);
                }
            }
        }
    }

    private static void ScheduleRetry(NotificationOutbox item)
    {
        if (item.AttemptCount >= EmailRetryPolicy.MaxRetryCount)
        {
            item.Status = "failed";
            item.NextAttemptAt = null;
            return;
        }

        item.AttemptCount++;
        item.Status = "pending";
        item.NextAttemptAt = DateTime.UtcNow.Add(EmailRetryPolicy.GetDelay(item.AttemptCount));
    }

    private static async Task ProcessBookingAsync(CinemaBookingDbContext db, IBookingEmailService email, NotificationOutbox item, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.BookingID == item.ReferenceId, ct);
        if (booking is null)
            return;

        if (booking.UserID.HasValue)
            await AddNotificationAsync(db, item, booking.UserID.Value, "Booking successful", $"Order  {booking.BookingCode} has been successfully paid for.", "booking", "Booking", $"/bookings/{booking.BookingID}", ct);

        await email.QueueBookingConfirmedAsync(booking.BookingID, ct);
    }
    private static async Task ProcessRefundAsync(CinemaBookingDbContext db, IBookingEmailService email, NotificationOutbox item, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingID == item.ReferenceId, ct);
        if (booking is null)
            return;

        if (booking.UserID.HasValue)
            await AddNotificationAsync(db, item, booking.UserID.Value, "Refund successful", $"Order {booking.BookingCode} has been refunded.", "refund", "Refund", $"/bookings/{booking.BookingID}", ct);

        await email.QueueRefundProcessedAsync(booking.BookingID, item.Amount!.Value, item.OccurredAt!.Value, ct);
    }

    // Manager/Admin notifications
    private static async Task ProcessRevenueAnomalyAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var cinemaId = item.ReferenceId;
        var roles = new[] { Roles.Manager };
        if (cinemaId == 0) roles = [.. roles, Roles.Admin];

        await notificationService.SendToRolesAsync(
            roles,
            "Revenue Alert",
            item.Message ?? "Revenue anomaly detected.",
            "analytics",
            "RevenueAnomaly",
            "Cinema",
            cinemaId,
            $"/admin/analytics/revenue?cinemaId={cinemaId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessDailySummaryAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var cinemaId = item.ReferenceId;
        var roles = new[] { Roles.Manager };
        if (cinemaId == 0) roles = [.. roles, Roles.Admin];

        await notificationService.SendToRolesAsync(
            roles,
            "Daily Summary",
            item.Message ?? "Daily summary report.",
            "report",
            "DailySummary",
            "Cinema",
            cinemaId,
            cinemaId > 0 ? $"/manager/reports/daily?cinemaId={cinemaId}" : "/admin/reports/daily",
            item.EventId,
            ct);
    }

    private static async Task ProcessTopSellingMovieAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var movieId = item.ReferenceId;
        var movie = await db.Movie.AsNoTracking().FirstOrDefaultAsync(x => x.MovieID == movieId, ct);
        var movieName = movie?.Title ?? "Movie";

        await notificationService.SendToRolesAsync(
            [Roles.Admin],
            "Top Selling Movie",
            item.Message ?? $"{movieName} is now the top selling movie.",
            "analytics",
            "TopSellingMovie",
            "Movie",
            movieId,
            $"/admin/movies/{movieId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessLowPerformingMovieAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var movieId = item.ReferenceId;
        var movie = await db.Movie.AsNoTracking().FirstOrDefaultAsync(x => x.MovieID == movieId, ct);
        var movieName = movie?.Title ?? "Movie";

        await notificationService.SendToRolesAsync(
            [Roles.Admin],
            "Low Performing Movie",
            item.Message ?? $"{movieName} has low occupancy.",
            "analytics",
            "LowPerformingMovie",
            "Movie",
            movieId,
            $"/admin/movies/{movieId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessMovieEndingSoonAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var movieId = item.ReferenceId;
        var movie = await db.Movie.AsNoTracking().FirstOrDefaultAsync(x => x.MovieID == movieId, ct);
        var movieName = movie?.Title ?? "Movie";

        await notificationService.SendToRolesAsync(
            [Roles.Admin],
            "Movie Ending Soon",
            item.Message ?? $"{movieName} will end its screening schedule soon.",
            "movie",
            "MovieEndingSoon",
            "Movie",
            movieId,
            $"/admin/movies/{movieId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessShowtimeSoldOutAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var showtimeId = item.ReferenceId;
        var showtime = await db.Showtimes.AsNoTracking().Include(x => x.Movie).FirstOrDefaultAsync(x => x.ShowtimeID == showtimeId, ct);
        var movieName = showtime?.Movie?.Title ?? "Movie";
        var showtimeStr = showtime?.StartTime.ToString("HH:mm") ?? "";

        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Showtime Sold Out",
            item.Message ?? $"Showtime {showtimeStr} - {movieName} has sold out.",
            "showtime",
            "ShowtimeSoldOut",
            "Showtime",
            showtimeId,
            $"/manager/showtimes/{showtimeId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessLowOccupancyShowtimeAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var showtimeId = item.ReferenceId;
        var showtime = await db.Showtimes.AsNoTracking().Include(x => x.Movie).FirstOrDefaultAsync(x => x.ShowtimeID == showtimeId, ct);
        var movieName = showtime?.Movie?.Title ?? "Movie";
        var showtimeStr = showtime?.StartTime.ToString("HH:mm") ?? "";

        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Low Occupancy Showtime",
            item.Message ?? $"Showtime {showtimeStr} - {movieName} has low occupancy.",
            "showtime",
            "LowOccupancyShowtime",
            "Showtime",
            showtimeId,
            $"/manager/showtimes/{showtimeId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessVoucherExpiringSoonAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var voucherId = item.ReferenceId;
        var voucher = await db.Vouchers.AsNoTracking().FirstOrDefaultAsync(x => x.VoucherID == voucherId, ct);
        var voucherCode = voucher?.VoucherCode ?? "Voucher";

        await notificationService.SendToRolesAsync(
            [Roles.Admin],
            "Voucher Expiring Soon",
            item.Message ?? $"Voucher {voucherCode} will expire soon.",
            "promotion",
            "VoucherExpiringSoon",
            "Voucher",
            voucherId,
            $"/admin/vouchers/{voucherId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessVoucherOutOfStockAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var voucherId = item.ReferenceId;
        var voucher = await db.Vouchers.AsNoTracking().FirstOrDefaultAsync(x => x.VoucherID == voucherId, ct);
        var voucherCode = voucher?.VoucherCode ?? "Voucher";

        await notificationService.SendToRolesAsync(
            [Roles.Admin],
            "Voucher Fully Redeemed",
            item.Message ?? $"Voucher {voucherCode} has reached its redemption limit.",
            "promotion",
            "VoucherOutOfStock",
            "Voucher",
            voucherId,
            $"/admin/vouchers/{voucherId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessNewCustomerSummaryAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        await notificationService.SendToRolesAsync(
            [Roles.Admin],
            "New Customer Report",
            item.Message ?? "New customer registration summary.",
            "system",
            "NewCustomerSummary",
            null,
            null,
            "/admin/users",
            item.EventId,
            ct);
    }

    private static async Task ProcessPaymentIssueAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        await notificationService.SendToRolesAsync(
            [Roles.Admin],
            "Payment Issue Detected",
            item.Message ?? "Payment failure rate has exceeded threshold.",
            "payment",
            "PaymentIssue",
            null,
            null,
            "/admin/payments/issues",
            item.EventId,
            ct);
    }

    private static async Task ProcessRoomInactiveAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var roomId = item.ReferenceId;
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.RoomID == roomId, ct);
        var roomName = room?.RoomName ?? $"Room {roomId}";

        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Room Marked Inactive",
            item.Message ?? $"{roomName} has been marked as inactive.",
            "system",
            "RoomInactive",
            "Room",
            roomId,
            $"/manager/rooms/{roomId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessRoomCreatedAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var roomId = item.ReferenceId;
        var room = await db.Rooms.AsNoTracking().Include(r => r.Cinema).FirstOrDefaultAsync(x => x.RoomID == roomId, ct);
        var roomName = room?.RoomName ?? $"Room {roomId}";
        var cinemaId = room?.CinemaID ?? 0;

        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Room Created",
            item.Message ?? $"{roomName} has been created.",
            "system",
            "RoomCreated",
            "Room",
            roomId,
            $"/manager/rooms/{roomId}?cinemaId={cinemaId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessRoomUpdatedAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var roomId = item.ReferenceId;
        var room = await db.Rooms.AsNoTracking().Include(r => r.Cinema).FirstOrDefaultAsync(x => x.RoomID == roomId, ct);
        var roomName = room?.RoomName ?? $"Room {roomId}";
        var cinemaId = room?.CinemaID ?? 0;

        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Room Updated",
            item.Message ?? $"{roomName} has been updated.",
            "system",
            "RoomUpdated",
            "Room",
            roomId,
            $"/manager/rooms/{roomId}?cinemaId={cinemaId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessShowtimeStartingSoonAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var showtimeId = item.ReferenceId;
        var showtime = await db.Showtimes.AsNoTracking().Include(x => x.Movie).FirstOrDefaultAsync(x => x.ShowtimeID == showtimeId, ct);
        var movieName = showtime?.Movie?.Title ?? "Movie";
        var showtimeStr = showtime?.StartTime.ToString("HH:mm") ?? "";

        await notificationService.SendToRolesAsync(
            [Roles.Staff],
            "Showtime Starting Soon",
            item.Message ?? $"Showtime {showtimeStr} - {movieName} will begin soon.",
            "showtime",
            "ShowtimeStartingSoon",
            "Showtime",
            showtimeId,
            $"/staff/showtimes/{showtimeId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessShowtimeCreatedAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var showtimeId = item.ReferenceId;
        var showtime = await db.Showtimes.AsNoTracking().Include(x => x.Movie).FirstOrDefaultAsync(x => x.ShowtimeID == showtimeId, ct);
        var movieName = showtime?.Movie?.Title ?? "Movie";
        var showtimeStr = showtime?.StartTime.ToString("HH:mm dd/MM/yyyy") ?? "";

        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Showtime Created",
            item.Message ?? $"Showtime for {movieName} at {showtimeStr} has been created.",
            "showtime",
            "ShowtimeCreated",
            "Showtime",
            showtimeId,
            $"/manager/showtimes/{showtimeId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessShowtimeUpdatedAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        var showtimeId = item.ReferenceId;
        var showtime = await db.Showtimes.AsNoTracking().Include(x => x.Movie).FirstOrDefaultAsync(x => x.ShowtimeID == showtimeId, ct);
        var movieName = showtime?.Movie?.Title ?? "Movie";
        var showtimeStr = showtime?.StartTime.ToString("HH:mm dd/MM/yyyy") ?? "";

        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Showtime Updated",
            item.Message ?? $"Showtime for {movieName} at {showtimeStr} has been updated.",
            "showtime",
            "ShowtimeUpdated",
            "Showtime",
            showtimeId,
            $"/manager/showtimes/{showtimeId}",
            item.EventId,
            ct);
    }

    private static async Task ProcessShowtimeDeletedAsync(CinemaBookingDbContext db, INotificationService notificationService, NotificationOutbox item, CancellationToken ct)
    {
        await notificationService.SendToRolesAsync(
            [Roles.Manager],
            "Showtime Deleted",
            item.Message ?? "A showtime has been deleted.",
            "showtime",
            "ShowtimeDeleted",
            "Showtime",
            item.ReferenceId,
            "/manager/showtimes",
            item.EventId,
            ct);
    }

    private static async Task AddNotificationAsync(CinemaBookingDbContext db, NotificationOutbox item, int userId, string title,
        string message, string type, string referenceType, string actionUrl, CancellationToken ct)
    {
        if (await db.Notifications.AnyAsync(x => x.EventId == item.EventId && x.UserID == userId, ct)) return;
        db.Notifications.Add(new Notification { UserID = userId, EventId = item.EventId, EventType = item.EventType,
            ReferenceType = referenceType, ReferenceId = item.ReferenceId, Title = title, Message = message, Type = type,
            ActionUrl = actionUrl, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }
}
