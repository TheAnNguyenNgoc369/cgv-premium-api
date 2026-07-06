using System.Threading.Channels;

namespace CinemaBooking.Infrastructure.Email;

public sealed class EmailQueueChannel
{
    private readonly Channel<EmailQueueItem> _channel = Channel.CreateUnbounded<EmailQueueItem>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    internal ValueTask WriteAsync(EmailQueueItem item, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    internal IAsyncEnumerable<EmailQueueItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
