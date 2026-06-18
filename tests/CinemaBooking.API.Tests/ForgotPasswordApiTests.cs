using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CinemaBooking.API.Tests;

public sealed class ForgotPasswordApiTests
{
    [Fact]
    public async Task ForgotPassword_WithUnverifiedAccount_ReturnsNotVerifiedAndDoesNotCreateToken()
    {
        var emailSender = new CapturingEmailSender();
        using var factory = CreateFactory(emailSender);
        using var client = factory.CreateClient();
        var email = $"unverified-{Guid.NewGuid():N}@cinema.com";

        await AddUserAsync(factory, email, emailVerifiedAt: null);

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Account is not verified.", document.RootElement.GetProperty("message").GetString());
        Assert.Empty(emailSender.Messages);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        Assert.Empty(dbContext.PasswordResetTokens.Where(t => t.User.Email == email));
    }

    [Fact]
    public async Task ForgotPassword_WhenCalledAgainWithinOneMinute_ReturnsCooldownAndKeepsExistingToken()
    {
        var emailSender = new CapturingEmailSender();
        using var factory = CreateFactory(emailSender);
        using var client = factory.CreateClient();
        var email = "c1@cinema.com";

        var firstResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email
        });

        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email
        });

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var document = await ReadJsonAsync(secondResponse);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(
            "Please wait before requesting another password reset email.",
            document.RootElement.GetProperty("message").GetString());
        Assert.Single(emailSender.Messages);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        Assert.Single(dbContext.PasswordResetTokens.Where(t => t.User.Email == email));
    }

    [Fact]
    public async Task ForgotPassword_AfterOneMinute_ReplacesUnusedTokenAndSendsNewEmail()
    {
        var emailSender = new CapturingEmailSender();
        using var factory = CreateFactory(emailSender);
        using var client = factory.CreateClient();
        var email = "c1@cinema.com";

        var firstResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email
        });

        firstResponse.EnsureSuccessStatusCode();

        int firstTokenId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
            var token = dbContext.PasswordResetTokens.Single(t => t.User.Email == email);
            firstTokenId = token.TokenID;
            token.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
            await dbContext.SaveChangesAsync();
        }

        var secondResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email
        });

        secondResponse.EnsureSuccessStatusCode();

        using var document = await ReadJsonAsync(secondResponse);
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(2, emailSender.Messages.Count);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var currentToken = Assert.Single(verifyDbContext.PasswordResetTokens.Where(t => t.User.Email == email));
        Assert.NotEqual(firstTokenId, currentToken.TokenID);
        Assert.Null(currentToken.UsedAt);
    }

    [Fact]
    public async Task ResetPassword_WithUsedToken_ReturnsAlreadyUsed()
    {
        var emailSender = new CapturingEmailSender();
        using var factory = CreateFactory(emailSender);
        using var client = factory.CreateClient();
        var token = $"reset-token-{Guid.NewGuid():N}";

        await AddResetTokenAsync(factory, "c1@cinema.com", token);

        var firstResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token,
            newPassword = "NewPassword@123",
            confirmPassword = "NewPassword@123"
        });

        firstResponse.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
            var usedToken = dbContext.PasswordResetTokens.Single(t => t.Token == token);
            Assert.NotNull(usedToken.UsedAt);
        }

        var secondResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token,
            newPassword = "AnotherPassword@123",
            confirmPassword = "AnotherPassword@123"
        });

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var document = await ReadJsonAsync(secondResponse);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Reset token has already been used", document.RootElement.GetProperty("message").GetString());
    }

    private static WebApplicationFactory<Program> CreateFactory(CapturingEmailSender emailSender)
    {
        return new CinemaBookingApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(emailSender);
            });
        });
    }

    private static async Task AddUserAsync(
        WebApplicationFactory<Program> factory,
        string email,
        DateTime? emailVerifiedAt)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var now = DateTime.UtcNow;

        dbContext.Users.Add(new User
        {
            FullName = "Forgot Password User",
            Email = email,
            Phone = "0912345678",
            PasswordHash = "unused",
            Role = "customer",
            Status = "active",
            EmailVerifiedAt = emailVerifiedAt,
            TotalPoints = 0,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task AddResetTokenAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string token)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var user = dbContext.Users.Single(u => u.Email == email);
        var now = DateTime.UtcNow;

        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserID = user.UserID,
            Token = token,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15)
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<(string ToEmail, string Subject, string HtmlBody)> Messages { get; } = [];

        public Task<bool> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((toEmail, subject, htmlBody));
            return Task.FromResult(true);
        }
    }
}
