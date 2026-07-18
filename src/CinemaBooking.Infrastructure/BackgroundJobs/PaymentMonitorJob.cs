using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class PaymentMonitorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentMonitorJob> _logger;

    private const double FailureRateThreshold = 0.5; // 50%
    private const int WindowMinutes = 10;
    private const int MinPaymentsForAlert = 5;

    public PaymentMonitorJob(IServiceScopeFactory scopeFactory, ILogger<PaymentMonitorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                await CheckPaymentFailuresAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to monitor payments");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckPaymentFailuresAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var windowStart = DateTime.UtcNow.AddMinutes(-WindowMinutes);

        var payments = await db.Payments
            .Where(p => p.CreatedAt >= windowStart)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new
            {
                Method = g.Key,
                Total = g.Count(),
                Failed = g.Count(p => p.Status == "failed"),
                Pending = g.Count(p => p.Status == "pending" 
                                    && p.CreatedAt < DateTime.UtcNow.AddMinutes(-15))
            })
            .ToListAsync(ct);

        foreach (var payment in payments)
        {
            if (payment.Total < MinPaymentsForAlert) continue;

            var failureRate = (double)payment.Failed / payment.Total;
            var staleRate = (double)payment.Pending / payment.Total;

            if (failureRate >= FailureRateThreshold || staleRate >= 0.3)
            {
                var eventId = $"PaymentIssue:{payment.Method}:{DateTime.UtcNow:yyyyMMddHHmm}";
                var alreadySent = await db.NotificationOutbox
                    .AnyAsync(x => x.EventId == eventId && x.Status == "processed", ct);

                if (alreadySent) continue;

                var message = $"Payment method {payment.Method} has issues: " +
                             $"Failure rate: {failureRate:P0} ({payment.Failed}/{payment.Total}), " +
                             $"Stale pending: {staleRate:P0} ({payment.Pending}/{payment.Total}).";

                await notificationOutbox.EnqueuePaymentIssueAsync(message, payment.Method, ct);
                _logger.LogWarning("Payment issue detected: {Method} - {Message}", payment.Method, message);
            }
        }
    }
}