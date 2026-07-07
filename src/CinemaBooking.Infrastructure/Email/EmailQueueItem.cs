using CinemaBooking.Application.Common.Interfaces;

namespace CinemaBooking.Infrastructure.Email;

internal sealed record EmailQueueItem(
    int EmailLogId,
    string ToEmail,
    string Subject,
    string HtmlBody,
    IReadOnlyCollection<EmailInlineImage>? InlineImages);
