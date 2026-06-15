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

        Assert.Equal(6, await dbContext.Users.CountAsync());
        Assert.Equal(6, await dbContext.Wallets.CountAsync());

        await CinemaBookingDbSeeder.SeedUsersAsync(scope.ServiceProvider);

        Assert.Equal(6, await dbContext.Users.CountAsync());
        Assert.Equal(6, await dbContext.Wallets.CountAsync());
    }
}
