using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class NewCustomerSummaryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewCustomerSummaryJob> _logger;

    public NewCustomerSummaryJob(IServiceScopeFactory scopeFactory, ILogger<NewCustomerSummaryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run at 00:10 daily
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddDays(1).AddMinutes(10);
        var delay = nextRun - now;

        await Task.Delay(delay, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                await SendNewCustomerSummaryAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send new customer summary");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendNewCustomerSummaryAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var startOfYesterday = yesterday.ToDateTime(TimeOnly.MinValue);
        var endOfYesterday = yesterday.ToDateTime(TimeOnly.MaxValue);

        var newCustomers = await db.Users
            .Where(u => u.Role == "customer"
                     && u.CreatedAt >= startOfYesterday
                     && u.CreatedAt <= endOfYesterday)
            .CountAsync(ct);

        var message = $"{newCustomers} new customer{(newCustomers != 1 ? "s" : "")} " +
                     $"registered on {yesterday:yyyy-MM-dd}.";

        await notificationOutbox.EnqueueNewCustomerSummaryAsync(message, ct);
        
        _logger.LogInformation("New customer summary sent: {Count} new customers on {Date}", 
            newCustomers, yesterday);
    }
}