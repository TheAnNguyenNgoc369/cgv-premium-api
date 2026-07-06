namespace CinemaBooking.Infrastructure.Email;

internal static class EmailRetryPolicy
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5)
    ];

    public static int MaxRetryCount => Delays.Length;

    public static TimeSpan GetDelay(int retryCount) => Delays[retryCount - 1];
}
