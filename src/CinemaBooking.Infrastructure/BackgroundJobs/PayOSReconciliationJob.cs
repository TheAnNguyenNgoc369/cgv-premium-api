using CinemaBooking.Application.Payments;
using CinemaBooking.Application.Payments.PayOS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class PayOSReconciliationJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PayOSReconciliationJob> _logger;

    public PayOSReconciliationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<PayOSReconciliationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConfirmWebhookAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ReconcilePendingPaymentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to reconcile pending PayOS payments");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ConfirmWebhookAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var payOSService = scope.ServiceProvider.GetRequiredService<IPayOSService>();
            await payOSService.ConfirmConfiguredWebhookAsync(cancellationToken);
            _logger.LogInformation("PayOS webhook confirmation completed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "PayOS webhook confirmation failed. Reconciliation job will continue.");
        }
    }

    private async Task ReconcilePendingPaymentsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var updatedCount = await paymentService.ReconcilePendingPayOSPaymentsAsync(
            BatchSize, cancellationToken);

        if (updatedCount > 0)
            _logger.LogInformation("Reconciled {PaymentCount} pending PayOS payments", updatedCount);
    }
}
