using CinemaBooking.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace CinemaBooking.API.Tests;

public sealed class MembershipAuthorizationTests
{
    [Fact]
    public void MembershipController_RequiresAuthenticationWithoutRoleRestriction()
    {
        var attribute = Assert.Single(
            typeof(MembershipController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Null(attribute.Roles);
        Assert.Null(attribute.Policy);
    }
}
