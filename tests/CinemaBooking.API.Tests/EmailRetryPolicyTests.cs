using CinemaBooking.Infrastructure.Email;

namespace CinemaBooking.API.Tests;

public sealed class EmailRetryPolicyTests
{
    [Fact]
    public void RetrySchedule_UsesConfiguredDelays()
    {
        Assert.Equal(3, EmailRetryPolicy.MaxRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(30), EmailRetryPolicy.GetDelay(1));
        Assert.Equal(TimeSpan.FromMinutes(2), EmailRetryPolicy.GetDelay(2));
        Assert.Equal(TimeSpan.FromMinutes(5), EmailRetryPolicy.GetDelay(3));
    }
}
