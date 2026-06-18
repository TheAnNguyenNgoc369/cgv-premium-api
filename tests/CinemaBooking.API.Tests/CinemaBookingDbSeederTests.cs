using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CinemaBooking.API.Tests;

public sealed class CinemaBookingDbSeederTests
{
    [Fact]
    public async Task SeedUsersAsync_CreatesDatabaseAndSeedsUsers_WhenDatabaseDoesNotExist()
    {
        var services = new ServiceCollection();
        services.AddDbContext<CinemaBookingDbContext>(options =>
            options.UseInMemoryDatabase($"SeederTests_{Guid.NewGuid():N}"));

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaBookingDbContext>();
        await dbContext.Database.EnsureDeletedAsync();

        await CinemaBookingDbSeeder.SeedUsersAsync(scope.ServiceProvider);

        const int expectedSeededUsers = 7;

        Assert.Equal(expectedSeededUsers, await dbContext.Users.CountAsync());
        Assert.Equal(expectedSeededUsers, await dbContext.Wallets.CountAsync());

        await CinemaBookingDbSeeder.SeedUsersAsync(scope.ServiceProvider);

        Assert.Equal(expectedSeededUsers, await dbContext.Users.CountAsync());
        Assert.Equal(expectedSeededUsers, await dbContext.Wallets.CountAsync());
    }
}
