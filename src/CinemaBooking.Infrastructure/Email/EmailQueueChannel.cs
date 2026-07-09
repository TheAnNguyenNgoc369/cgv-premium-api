using System.Threading.Channels;

namespace CinemaBooking.Infrastructure.Email;

public sealed class EmailQueueChannel
{
    private const int Capacity = 1000;

    private readonly Channel<EmailQueueItem> _channel = Channel.CreateBounded<EmailQueueItem>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    internal ValueTask WriteAsync(EmailQueueItem item, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    internal IAsyncEnumerable<EmailQueueItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
