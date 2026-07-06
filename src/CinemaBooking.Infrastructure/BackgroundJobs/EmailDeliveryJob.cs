using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Infrastructure.Email;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class EmailDeliveryJob : BackgroundService
{
    private const string DeliveryFailedMessage = "Email delivery failed.";
    private readonly EmailQueueChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailDeliveryJob> _logger;

    public EmailDeliveryJob(
        EmailQueueChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailDeliveryJob> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.ReadAllAsync(stoppingToken))
            await ProcessAsync(item, stoppingToken);
    }

    private async Task ProcessAsync(EmailQueueItem item, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var emailLog = await dbContext.EmailLogs.SingleOrDefaultAsync(
            log => log.EmailLogID == item.EmailLogId,
            cancellationToken);

        if (emailLog is null || emailLog.DeliveryStatus is "sent" or "failed")
            return;

        emailLog.DeliveryStatus = "processing";
        await dbContext.SaveChangesAsync(cancellationToken);

        bool sent;
        try
        {
            sent = await emailSender.SendAsync(
                item.ToEmail,
                item.Subject,
                item.HtmlBody,
                item.InlineImages,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            sent = false;
            _logger.LogError(exception, "Unexpected failure delivering email log {EmailLogId}", item.EmailLogId);
        }

        if (sent)
        {
            emailLog.DeliveryStatus = "sent";
            emailLog.SentAt = DateTime.UtcNow;
            emailLog.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        emailLog.LastError = DeliveryFailedMessage;
        if (emailLog.RetryCount >= EmailRetryPolicy.MaxRetryCount)
        {
            emailLog.DeliveryStatus = "failed";
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError("Email log {EmailLogId} failed after {RetryCount} retries", item.EmailLogId, emailLog.RetryCount);
            return;
        }

        emailLog.RetryCount++;
        emailLog.DeliveryStatus = "retrying";
        await dbContext.SaveChangesAsync(cancellationToken);
        _ = RetryLaterAsync(item, emailLog.RetryCount, cancellationToken);
    }

    private async Task RetryLaterAsync(
        EmailQueueItem item,
        int retryCount,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(EmailRetryPolicy.GetDelay(retryCount), cancellationToken);
            await _channel.WriteAsync(item, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Application shutdown intentionally abandons this in-memory retry.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to requeue email log {EmailLogId}", item.EmailLogId);
        }
    }
}
