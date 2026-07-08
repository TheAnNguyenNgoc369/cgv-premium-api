using CinemaBooking.API.Configuration;
using CinemaBooking.API.Controllers;
using CinemaBooking.API.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaBooking.API.Tests;

public sealed class AuthRateLimitContractTests
{
    [Theory]
    [InlineData(nameof(AuthController.Login), AuthRateLimitPolicyNames.Login)]
    [InlineData(nameof(AuthController.Register), AuthRateLimitPolicyNames.Register)]
    [InlineData(nameof(AuthController.ForgotPassword), AuthRateLimitPolicyNames.EmailAction)]
    [InlineData(nameof(AuthController.ResendVerificationEmail), AuthRateLimitPolicyNames.EmailAction)]
    [InlineData(nameof(AuthController.VerifyEmail), AuthRateLimitPolicyNames.Verify)]
    public void SensitiveAuthEndpoints_HaveIpRateLimitPolicy(string actionName, string policyName)
    {
        var method = typeof(AuthController).GetMethods()
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(
            method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
                .Cast<EnableRateLimitingAttribute>());

        Assert.Equal(policyName, attribute.PolicyName);
    }

    [Fact]
    public async Task AuthRequestRateLimiter_LimitsRepeatedEmailAttempts()
    {
        using var limiter = new AuthRequestRateLimiter();

        for (var i = 0; i < 5; i++)
        {
            Assert.True(await limiter.TryAcquireAsync("login", "USER@example.com"));
        }

        Assert.False(await limiter.TryAcquireAsync("login", "user@example.com"));
    }
}
