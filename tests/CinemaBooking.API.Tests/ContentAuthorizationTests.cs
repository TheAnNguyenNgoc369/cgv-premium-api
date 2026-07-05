using CinemaBooking.API.Controllers;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.API.Tests;

public sealed class ContentAuthorizationTests
{
    [Theory]
    [InlineData(nameof(GenreController.GetGenres))]
    [InlineData(nameof(GenreController.GetGenreById))]
    public void GenreReads_AllowAnonymous(string methodName)
    {
        var method = typeof(GenreController).GetMethod(methodName)!;
        Assert.NotNull(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
    }

    [Theory]
    [InlineData(nameof(GenreController.CreateGenre))]
    [InlineData(nameof(GenreController.UpdateGenre))]
    [InlineData(nameof(GenreController.DeleteGenre))]
    public void GenreWrites_RequireAdmin(string methodName)
    {
        var method = typeof(GenreController).GetMethod(methodName)!;
        var attribute = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(Roles.Admin, attribute.Roles);
    }

    [Theory]
    [InlineData(nameof(SeatTypeController.GetSeatTypes))]
    [InlineData(nameof(SeatTypeController.GetSeatTypeById))]
    public void SeatTypeReads_RequireManager(string methodName)
    {
        var method = typeof(SeatTypeController).GetMethod(methodName)!;
        var attribute = Assert.Single(method
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(Roles.Manager, attribute.Roles);
    }

    [Theory]
    [InlineData(nameof(SeatTypeController.CreateSeatType))]
    [InlineData(nameof(SeatTypeController.UpdateSeatType))]
    [InlineData(nameof(SeatTypeController.DeleteSeatType))]
    public void SeatTypeWrites_RequireAdmin(string methodName)
    {
        var method = typeof(SeatTypeController).GetMethod(methodName)!;
        var attribute = Assert.Single(method
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(Roles.Admin, attribute.Roles);
    }

    [Fact]
    public void VoucherGet_AllowsAnonymous()
    {
        var method = typeof(VoucherController).GetMethod(nameof(VoucherController.Get))!;
        Assert.Single(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    [Theory]
    [InlineData(nameof(VoucherController.Create))]
    [InlineData(nameof(VoucherController.Update))]
    [InlineData(nameof(VoucherController.Delete))]
    public void VoucherWrites_RequireAdmin(string methodName)
    {
        var method = typeof(VoucherController).GetMethods().Single(method => method.Name == methodName);
        var attribute = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(Roles.Admin, attribute.Roles);
    }

    [Fact]
    public void Reports_RequireAdminOrManager()
    {
        var attribute = Assert.Single(typeof(ReportsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal($"{Roles.Admin},{Roles.Manager}", attribute.Roles);
    }
}
