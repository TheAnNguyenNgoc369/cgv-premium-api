using System.Text.Json;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Infrastructure.Email;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.Infrastructure.BackgroundJobs;

public sealed class EmailDeliveryJob(IServiceScopeFactory scopeFactory, ILogger<EmailDeliveryJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = DateTime.UtcNow;
        var logs = await db.EmailLogs.Where(x => (x.DeliveryStatus == "pending" || x.DeliveryStatus == "retrying") && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var log in logs)
        {
            log.DeliveryStatus = "processing"; await db.SaveChangesAsync(ct);
            try
            {
                var images = log.InlineImagesJson is null ? null : JsonSerializer.Deserialize<List<EmailInlineImage>>(log.InlineImagesJson);
                if (await sender.SendAsync(log.ToEmail, log.Subject, log.HtmlBody, images, ct))
                { log.DeliveryStatus = "sent"; log.SentAt = DateTime.UtcNow; log.LastError = null; log.NextAttemptAt = null; }
                else ScheduleRetry(log);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { log.LastError = ex.Message; ScheduleRetry(log); logger.LogError(ex, "Email log {EmailLogId} failed", log.EmailLogID); }
            await db.SaveChangesAsync(ct);
        }
    }
    private static void ScheduleRetry(Domain.Entities.EmailLog log)
    {
        log.LastError ??= "Email delivery failed.";
        if (log.RetryCount >= EmailRetryPolicy.MaxRetryCount)
        {
            log.DeliveryStatus = "failed";
            log.NextAttemptAt = null;
            return;
        }
        log.RetryCount++;
        log.DeliveryStatus = "retrying";
        log.NextAttemptAt = DateTime.UtcNow.Add(EmailRetryPolicy.GetDelay(log.RetryCount));
    }
}
