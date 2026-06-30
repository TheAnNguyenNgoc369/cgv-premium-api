using CinemaBooking.API.Controllers;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.API.Tests;

public sealed class CinemaAuthorizationTests
{
    [Theory]
    [InlineData(nameof(CinemaController.GetCinemas))]
    [InlineData(nameof(CinemaController.GetCinemaById))]
    public void ReadEndpoints_AllowAnonymous(string methodName)
    {
        var method = typeof(CinemaController).GetMethod(methodName)!;

        Assert.NotNull(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
    }

    [Theory]
    [InlineData(nameof(CinemaController.CreateCinema))]
    [InlineData(nameof(CinemaController.UpdateCinema))]
    [InlineData(nameof(CinemaController.DeleteCinema))]
    public void WriteEndpoints_RequireManagerOrAdmin(string methodName)
    {
        var method = typeof(CinemaController).GetMethod(methodName)!;
        var attribute = Assert.Single(
            method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Equal($"{Roles.Manager},{Roles.Admin}", attribute.Roles);
    }
}
