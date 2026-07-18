using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class ShowtimeCompletionJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShowtimeCompletionJob> _logger;

    public ShowtimeCompletionJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ShowtimeCompletionJob> logger)
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
                await CompleteShowtimesAndMarkNoShowAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to complete showtimes and mark NoShow bookings");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task CompleteShowtimesAndMarkNoShowAsync(
        CancellationToken cancellationToken = default)
    {
        await using var strategyScope = _scopeFactory.CreateAsyncScope();
        var strategyContext = strategyScope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var completedShowtimeCount = 0;
        var noShowMarkedCount = 0;

        await strategy.ExecuteAsync(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
            var now = DateTime.UtcNow;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            completedShowtimeCount = await dbContext.Showtimes
                .Where(showtime => showtime.Status == "scheduled" && showtime.EndTime <= now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(showtime => showtime.Status, "completed"),
                    cancellationToken);

            noShowMarkedCount = await MarkNoShowBookingsAsync(dbContext, now, cancellationToken);
            var eligibleBookings = await dbContext.Bookings
                .Include(booking => booking.User)
                .Where(booking => booking.Status == BookingStatus.Paid
                    && booking.UserID.HasValue
                    && booking.Showtime!.Status == "completed"
                    && !booking.LoyaltyPoints.Any(point =>
                        point.TransactionType == LoyaltyTransactionTypes.Earned))
                .ToListAsync(cancellationToken);

            processedBookingCount = eligibleBookings.Count;

            if (eligibleBookings.Count > 0)
            {
                var tiers = await dbContext.LoyaltyTiers
                    .OrderBy(tier => tier.MinPoints)
                    .ToListAsync(cancellationToken);

                foreach (var booking in eligibleBookings)
                {
                    var pointsEarned = (int)(booking.FinalAmount * MembershipTiers.PointsPerVnd);
                    if (pointsEarned <= 0)
                        continue;

                    booking.PointsEarned = pointsEarned;
                    booking.User!.TotalPoints += pointsEarned;
                    booking.User!.UpdatedAt = now;
                    booking.User!.LoyaltyTierID = tiers
                        .Where(tier => booking.User!.TotalPoints >= tier.MinPoints)
                        .OrderByDescending(tier => tier.MinPoints)
                        .Select(tier => (int?)tier.TierID)
                        .FirstOrDefault();

                    dbContext.LoyaltyPoints.Add(new LoyaltyPoints
                    {
                        UserID = booking.UserID.GetValueOrDefault(),
                        BookingID = booking.BookingID,
                        PointsDelta = pointsEarned,
                        TransactionType = LoyaltyTransactionTypes.Earned,
                        Description = $"Earned {pointsEarned} points after showtime completion",
                        CreatedAt = now
                    });
                }

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "EXEC sys.sp_set_session_context @key=N'SkipLoyaltyPointTrigger', @value=1",
                        cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                finally
                {
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "EXEC sys.sp_set_session_context @key=N'SkipLoyaltyPointTrigger', @value=NULL",
                        CancellationToken.None);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        });

        if (completedShowtimeCount > 0 || noShowMarkedCount > 0)
        {
            _logger.LogInformation(
                "Completed {ShowtimeCount} showtimes and marked {NoShowCount} bookings as NoShow",
                completedShowtimeCount,
                noShowMarkedCount);
        }
    }

    private async Task<int> MarkNoShowBookingsAsync(
        CinemaBookingDbContext dbContext,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var noShowCandidates = await dbContext.Bookings
            .Where(booking => booking.Status == BookingStatus.Paid
                && booking.Showtime.Status == "completed"
                && !booking.BookingSeats.Any(seat =>
                    seat.Ticket != null && seat.Ticket.Status == TicketStatus.Used))
            .ToListAsync(cancellationToken);

        if (noShowCandidates.Count == 0)
            return 0;

        foreach (var booking in noShowCandidates)
        {
            _logger.LogInformation(
                "Marking booking as NoShow BookingId={BookingId} BookingCode={BookingCode} OldStatus={OldStatus} NewStatus={NewStatus}",
                booking.BookingID,
                booking.BookingCode,
                booking.Status,
                BookingStatus.NoShow);

            booking.Status = BookingStatus.NoShow;
            booking.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return noShowCandidates.Count;
    }
}
