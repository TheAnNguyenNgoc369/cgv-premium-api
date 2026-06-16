using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CinemaBooking.API.Tests;

public sealed class UserProfileApiTests
{
    [Fact]
    public async Task GetProfile_WithValidToken_ReturnsCurrentUserWithoutSensitiveFields()
    {
        using var factory = new CinemaBookingApiFactory();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.GetAsync("/api/user/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        var profile = document.RootElement;

        Assert.Equal("c1@cinema.com", profile.GetProperty("email").GetString());
        Assert.Equal("customer", profile.GetProperty("role").GetString());
        Assert.False(profile.TryGetProperty("password", out _));
        Assert.False(profile.TryGetProperty("passwordHash", out _));
        Assert.False(profile.TryGetProperty("token", out _));
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new CinemaBookingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/user/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithValidInput_UpdatesCurrentUser()
    {
        using var factory = new CinemaBookingApiFactory();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/user/profile", new
        {
            fullName = "Updated Customer",
            phone = "0900000999",
            avatarURL = "https://example.com/avatar.png"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        var profile = document.RootElement;

        Assert.Equal("Updated Customer", profile.GetProperty("fullName").GetString());
        Assert.Equal("0900000999", profile.GetProperty("phone").GetString());
        Assert.Equal("https://example.com/avatar.png", profile.GetProperty("avatarURL").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithAnotherUsersPhone_UpdatesCurrentUser()
    {
        using var factory = new CinemaBookingApiFactory();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/user/profile", new
        {
            fullName = "Customer One",
            phone = "0900000004"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        Assert.Equal("0900000004", document.RootElement.GetProperty("phone").GetString());
    }

    [Fact]
    public void UserModel_PhoneDoesNotHaveUniqueIndex()
    {
        using var factory = new CinemaBookingApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var userEntityType = dbContext.Model.FindEntityType(typeof(User));

        Assert.NotNull(userEntityType);
        Assert.DoesNotContain(
            userEntityType.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Any(property => property.Name == nameof(User.Phone)));
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidInput_ReturnsBadRequest()
    {
        using var factory = new CinemaBookingApiFactory();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/user/profile", new
        {
            fullName = "Invalid Phone",
            phone = "12345"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithSensitiveFields_DoesNotUpdateThem()
    {
        using var factory = new CinemaBookingApiFactory();
        using var client = await CreateAuthorizedClientAsync(factory);

        var initialProfileResponse = await client.GetAsync("/api/user/profile");
        initialProfileResponse.EnsureSuccessStatusCode();

        using var initialProfileDocument = await ReadJsonAsync(initialProfileResponse);
        var originalUserId = initialProfileDocument.RootElement.GetProperty("userID").GetInt32();
        var originalCreatedAt = initialProfileDocument.RootElement.GetProperty("createdAt").GetDateTime();

        var response = await client.PutAsJsonAsync("/api/user/profile", new Dictionary<string, object?>
        {
            ["fullName"] = "Sensitive Fields Attempt",
            ["phone"] = "0900000998",
            ["role"] = "admin",
            ["status"] = "locked",
            ["passwordHash"] = "hacked",
            ["userID"] = 999,
            ["createdAt"] = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        var profile = document.RootElement;

        Assert.Equal(originalUserId, profile.GetProperty("userID").GetInt32());
        Assert.Equal("customer", profile.GetProperty("role").GetString());
        Assert.Equal("active", profile.GetProperty("status").GetString());
        Assert.Equal(originalCreatedAt, profile.GetProperty("createdAt").GetDateTime());
        Assert.False(profile.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task VerifyEmailPostEndpoint_IsNotAvailableAndNotDocumented()
    {
        using var factory = new CinemaBookingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = "123456"
        });

        Assert.Contains(response.StatusCode, new[]
        {
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed
        });

        var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
        swaggerResponse.EnsureSuccessStatusCode();

        using var document = await ReadJsonAsync(swaggerResponse);
        var paths = document.RootElement.GetProperty("paths");

        if (paths.TryGetProperty("/api/auth/verify-email", out var verifyEmailPath))
        {
            Assert.False(verifyEmailPath.TryGetProperty("post", out _));
        }
    }

    private static async Task<HttpClient> CreateAuthorizedClientAsync(
        CinemaBookingApiFactory factory)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "c1@cinema.com",
            password = "Password@123"
        });

        response.EnsureSuccessStatusCode();

        using var document = await ReadJsonAsync(response);

        return document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Login response did not include a token.");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
