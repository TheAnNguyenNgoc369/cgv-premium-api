using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

public sealed class ImageUploadApiTests
{
    [Fact]
    public async Task UploadAvatar_WithValidImage_UpdatesDatabaseAndDeletesOldImage()
    {
        var imageStorage = new FakeImageStorageService();
        using var factory = CreateFactory(imageStorage);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "c1@cinema.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
            var user = dbContext.Users.Single(u => u.Email == "c1@cinema.com");
            user.AvatarURL = "https://res.cloudinary.com/demo/old-avatar.jpg";
            user.AvatarPublicId = "old-avatar";
            await dbContext.SaveChangesAsync();
        }

        using var content = CreateImageContent("avatar.png", "image/png");
        var response = await client.PutAsync("/api/users/profile/avatar", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        Assert.Equal("https://cdn.example.com/cgvp/avatars/avatar.png", document.RootElement.GetProperty("secureUrl").GetString());
        Assert.Equal("cgvp/avatars/avatar", document.RootElement.GetProperty("publicId").GetString());
        Assert.Contains("old-avatar", imageStorage.DeletedPublicIds);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var updatedUser = verifyDbContext.Users.Single(u => u.Email == "c1@cinema.com");
        Assert.Equal("https://cdn.example.com/cgvp/avatars/avatar.png", updatedUser.AvatarURL);
        Assert.Equal("cgvp/avatars/avatar", updatedUser.AvatarPublicId);
    }

    [Fact]
    public async Task UploadAvatar_WithInvalidFileType_ReturnsBadRequest()
    {
        var imageStorage = new FakeImageStorageService();
        using var factory = CreateFactory(imageStorage);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "c1@cinema.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = CreateImageContent("avatar.gif", "image/gif");
        var response = await client.PutAsync("/api/users/profile/avatar", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(imageStorage.UploadedPublicIds);
    }

    [Fact]
    public async Task DeleteAvatar_RemovesDatabaseValuesAndDeletesCloudinaryImage()
    {
        var imageStorage = new FakeImageStorageService();
        using var factory = CreateFactory(imageStorage);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "c1@cinema.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
            var user = dbContext.Users.Single(u => u.Email == "c1@cinema.com");
            user.AvatarURL = "https://res.cloudinary.com/demo/current-avatar.jpg";
            user.AvatarPublicId = "current-avatar";
            await dbContext.SaveChangesAsync();
        }

        var response = await client.DeleteAsync("/api/users/profile/avatar");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("current-avatar", imageStorage.DeletedPublicIds);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var updatedUser = verifyDbContext.Users.Single(u => u.Email == "c1@cinema.com");
        Assert.Null(updatedUser.AvatarURL);
        Assert.Null(updatedUser.AvatarPublicId);
    }

    [Fact]
    public async Task UserController_DoesNotExposeDuplicateUsersProfileRoute()
    {
        var imageStorage = new FakeImageStorageService();
        using var factory = CreateFactory(imageStorage);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "c1@cinema.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadMoviePoster_WithAdminAndValidImage_UpdatesDatabaseAndDeletesOldImage()
    {
        var imageStorage = new FakeImageStorageService();
        using var factory = CreateFactory(imageStorage);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin@cinema.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var movieId = await AddMovieAsync(
            factory,
            posterUrl: "https://res.cloudinary.com/demo/old-poster.jpg",
            posterPublicId: "old-poster");

        using var content = CreateImageContent("poster.webp", "image/webp");
        var response = await client.PutAsync($"/api/movies/{movieId}/poster", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("old-poster", imageStorage.DeletedPublicIds);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var movie = verifyDbContext.Movies.Single(m => m.MovieID == movieId);
        Assert.Equal("https://cdn.example.com/cgvp/movie-posters/poster.webp", movie.PosterURL);
        Assert.Equal("cgvp/movie-posters/poster", movie.PosterPublicId);
    }

    [Fact]
    public async Task UploadMoviePoster_WithCustomer_ReturnsForbidden()
    {
        var imageStorage = new FakeImageStorageService();
        using var factory = CreateFactory(imageStorage);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "c1@cinema.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var movieId = await AddMovieAsync(factory);

        using var content = CreateImageContent("poster.jpg", "image/jpeg");
        var response = await client.PutAsync($"/api/movies/{movieId}/poster", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(imageStorage.UploadedPublicIds);
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeImageStorageService imageStorage)
    {
        return new CinemaBookingApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IImageStorageService>();
                services.AddSingleton<IImageStorageService>(imageStorage);
            });
        });
    }

    private static MultipartFormDataContent CreateImageContent(
        string fileName,
        string contentType)
    {
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image bytes"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        multipartContent.Add(fileContent, "file", fileName);

        return multipartContent;
    }

    private static async Task<int> AddMovieAsync(
        WebApplicationFactory<Program> factory,
        string? posterUrl = null,
        string? posterPublicId = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        var now = DateTime.UtcNow;
        var movie = new Movie
        {
            Title = $"Upload Test Movie {Guid.NewGuid():N}",
            AgeRating = "P",
            PosterURL = posterUrl,
            PosterPublicId = posterPublicId,
            DurationMin = 120,
            Status = "coming_soon",
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Movies.Add(movie);
        await dbContext.SaveChangesAsync();

        return movie.MovieID;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
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

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public List<string> UploadedPublicIds { get; } = [];
        public List<string> DeletedPublicIds { get; } = [];

        public Task<StoredImageResult> UploadImageAsync(
            Stream imageStream,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default)
        {
            var publicId = $"{folder}/{Path.GetFileNameWithoutExtension(fileName)}";
            UploadedPublicIds.Add(publicId);

            return Task.FromResult(new StoredImageResult(
                $"https://cdn.example.com/{folder}/{fileName}",
                publicId));
        }

        public Task DeleteImageAsync(
            string publicId,
            CancellationToken cancellationToken = default)
        {
            DeletedPublicIds.Add(publicId);
            return Task.CompletedTask;
        }
    }
}
