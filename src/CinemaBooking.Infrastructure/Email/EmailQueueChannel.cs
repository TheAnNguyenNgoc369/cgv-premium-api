using System.Threading.Channels;

namespace CinemaBooking.Infrastructure.Email;

internal sealed class EmailQueueChannel
{
    private readonly Channel<EmailQueueItem> _channel = Channel.CreateUnbounded<EmailQueueItem>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask WriteAsync(EmailQueueItem item, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    public IAsyncEnumerable<EmailQueueItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
