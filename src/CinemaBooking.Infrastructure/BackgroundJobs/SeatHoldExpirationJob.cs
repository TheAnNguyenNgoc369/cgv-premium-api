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
            booking.Status = BookingStatus.Expired;
            booking.UpdatedAt = now;

            foreach (var item in booking.BookingFnBs)
            {
                var product = await dbContext.Products.FindAsync(
                    new object[] { item.ItemID }, cancellationToken);
                if (product is null)
                    continue;

                product.StockQuantity += item.Quantity;
                product.UpdatedAt = now;
                product.Status = product.StockQuantity > 10 ? "in_stock" : "low_stock";
            }

            if (booking.BookingVoucher is not null)
            {
                var voucher = await dbContext.Vouchers.FindAsync(
                    new object[] { booking.BookingVoucher.VoucherID }, cancellationToken);
                if (voucher is not null && voucher.UsedCount > 0)
                    voucher.UsedCount--;
            }

            if (booking.Payment is not null)
            {
                if (booking.Payment.Status == PaymentStatus.Pending)
                    booking.Payment.Status = PaymentStatus.Expired;
                foreach (var session in booking.Payment.PaymentSessions)
                    session.Status = "expired";
            }
        }

        var expiredCount = await dbContext.SeatHolds
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

        if (expiredCount > 0)
            _logger.LogInformation("Expired {SeatHoldCount} seat holds", expiredCount);
    }
}
