using CinemaBooking.API.Controllers;
using CinemaBooking.Shared.Constants;
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

    [Fact]
    public void AdminLoyaltyTierController_RequiresAdminRole()
    {
        var attribute = Assert.Single(
            typeof(AdminLoyaltyTierController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(Roles.Admin, attribute.Roles);
        Assert.Null(attribute.Policy);
    }
}
