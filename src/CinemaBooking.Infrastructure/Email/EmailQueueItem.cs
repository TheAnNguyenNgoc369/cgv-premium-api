namespace CinemaBooking.Infrastructure.Email;

internal sealed record EmailQueueItem(
    int EmailLogId,
    string ToEmail,
    string Subject,
    string HtmlBody);
