using CinemaBooking.API.Controllers;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.API.Tests;

public sealed class NotificationContractTests
{
    [Fact]
    public void Controller_RequiresCustomerOrStaff()
    {
        var attribute = Assert.Single(typeof(NotificationController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal($"{Roles.Customer},{Roles.Staff}", attribute.Roles);
    }

    [Fact]
    public void Model_HasDurableOutboxAndUniqueNotificationEventKey()
    {
        var options = new DbContextOptionsBuilder<CinemaBookingDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=contract-only;Trusted_Connection=True")
            .Options;
        using var db = new CinemaBookingDbContext(options);
        var outbox = db.Model.FindEntityType(typeof(NotificationOutbox));
        Assert.NotNull(outbox);
        var notification = db.Model.FindEntityType(typeof(Notification))!;
        Assert.Contains(notification.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(["EventId", "UserID"]));

        var emailLog = db.Model.FindEntityType(typeof(EmailLog))!;
        Assert.Contains(emailLog.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(["EventId"]));
    }
}
