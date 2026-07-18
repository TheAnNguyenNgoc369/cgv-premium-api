using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class VoucherExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VoucherExpiryJob> _logger;
    private readonly VoucherExpirySettings _settings;

    public VoucherExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<VoucherExpiryJob> logger,
        IOptions<VoucherExpirySettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run immediately on start, then daily
        await CheckExpiringVouchersAsync(stoppingToken);
        
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckExpiringVouchersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to check expiring vouchers");
            }
        }
    }

    private async Task CheckExpiringVouchersAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var notificationOutbox = scope.ServiceProvider.GetRequiredService<INotificationOutbox>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var days in _settings.ReminderDays)
        {
            var targetDate = today.AddDays(days);

            var expiringVouchers = await db.Vouchers
                .Where(v => v.IsActive
                         && v.ValidUntil.Date == targetDate.ToDateTime(TimeOnly.MinValue))
                .Select(v => new { v.VoucherID, v.VoucherCode, DaysLeft = days })
                .ToListAsync(ct);

            foreach (var voucher in expiringVouchers)
            {
                var message = $"Voucher {voucher.VoucherCode} will expire in {voucher.DaysLeft} day{(voucher.DaysLeft > 1 ? "s" : "")}.";
                
                await notificationOutbox.EnqueueVoucherExpiringSoonAsync(
                    voucher.VoucherID,
                    message,
                    voucher.DaysLeft,
                    ct);
            }
        }
    }
}

public sealed class VoucherExpirySettings
{
    public const string SectionName = "VoucherExpiry";
    
    /// <summary>Days before expiry to send reminders (e.g., 7, 3, 1)</summary>
    public int[] ReminderDays { get; set; } = { 7, 3, 1 };
}