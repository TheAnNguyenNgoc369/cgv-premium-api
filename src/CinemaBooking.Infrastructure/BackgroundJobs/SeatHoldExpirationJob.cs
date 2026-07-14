using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class SeatHoldExpirationJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatHoldExpirationJob> _logger;

    public SeatHoldExpirationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SeatHoldExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ExpireHoldsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to expire seat holds");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireHoldsAsync(CancellationToken cancellationToken)
    {
        await using var strategyScope = _scopeFactory.CreateAsyncScope();
        var strategyContext = strategyScope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var expiredCount = 0;

        await strategy.ExecuteAsync(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
            var now = DateTime.UtcNow;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var expiredBookingIds = await dbContext.SeatHolds
                .Where(hold => hold.Status == SeatHoldStatus.Confirmed
                    && hold.ExpiresAt <= now
                    && hold.BookingID.HasValue)
                .Select(hold => hold.BookingID!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var bookings = await dbContext.Bookings
                .Include(booking => booking.BookingFnBs)
                .Include(booking => booking.BookingVoucher)
                .Include(booking => booking.Payment).ThenInclude(payment => payment!.PaymentSessions)
                .AsSplitQuery()
                .Where(booking => expiredBookingIds.Contains(booking.BookingID)
                    && (booking.Status == BookingStatus.Pending
                        || booking.Status == BookingStatus.PaymentFailed))
                .ToListAsync(cancellationToken);

            foreach (var booking in bookings)
            {
                // Concurrency guard: re-check booking status with a row lock before releasing
                // the voucher reservation. A concurrent payment webhook may have flipped
                // Pending -> Paid; in that case we must leave the UserVoucher marked Used.
                // Voucher.UsedCount is not touched here: for public vouchers it is only
                // incremented on payment success, for loyalty it is incremented at redeem —
                // an expired booking never contributed to the counter.
                var currentStatus = await dbContext.Bookings
                    .FromSqlRaw("SELECT * FROM Booking WITH (UPDLOCK, ROWLOCK) WHERE BookingID = {0}",
                        booking.BookingID)
                    .Select(b => b.Status)
                    .FirstOrDefaultAsync(cancellationToken);

                if (currentStatus != BookingStatus.Pending
                    && currentStatus != BookingStatus.PaymentFailed)
                {
                    // Payment completed concurrently — leave voucher accounting intact.
                    continue;
                }

                booking.Status = BookingStatus.Expired;
                booking.UpdatedAt = now;

                if (booking.BookingVoucher is not null)
                {
                    // Loyalty voucher: release the personal reservation by clearing BookingID.
                    // Filter on Status == Available so a concurrently-Used voucher stays Used.
                    // (A "reserved" UserVoucher is one where BookingID is set and Status is still Available;
                    //  the CHECK constraint on Status has no separate Reserved value.)
                    await dbContext.Set<UserVoucher>()
                        .Where(uv => uv.BookingID == booking.BookingID
                            && uv.Status == UserVoucherStatus.Available)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(uv => uv.BookingID, (int?)null), cancellationToken);
                }

                if (booking.Payment is not null)
                {
                    if (booking.Payment.Status == PaymentStatus.Pending)
                        booking.Payment.Status = PaymentStatus.Expired;
                    foreach (var session in booking.Payment.PaymentSessions)
                        session.Status = "expired";
                }
            }

            expiredCount = await dbContext.SeatHolds
                .Where(hold => hold.Status == SeatHoldStatus.Holding
                        || hold.Status == SeatHoldStatus.Confirmed)
                .Where(hold => hold.ExpiresAt <= now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        hold => hold.Status,
                        SeatHoldStatus.Expired),
                    cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        if (expiredCount > 0)
            _logger.LogInformation("Expired {SeatHoldCount} seat holds", expiredCount);
    }
}
