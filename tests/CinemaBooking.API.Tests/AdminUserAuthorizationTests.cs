using CinemaBooking.API.Controllers;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.API.Tests;

public sealed class AdminUserAuthorizationTests
{
    [Fact]
    public void Controller_RequiresAdminRole()
    {
        var attribute = Assert.Single(
            typeof(AdminUserController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(Roles.Admin, attribute.Roles);
    }
}
