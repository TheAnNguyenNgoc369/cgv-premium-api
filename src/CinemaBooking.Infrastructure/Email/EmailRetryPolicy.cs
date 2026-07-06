namespace CinemaBooking.Infrastructure.Email;

internal static class EmailRetryPolicy
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15)
    ];

    public static int MaxRetryCount => Delays.Length;

    public static TimeSpan GetDelay(int retryCount) => Delays[retryCount - 1];
}
