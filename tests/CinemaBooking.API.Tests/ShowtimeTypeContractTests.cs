using CinemaBooking.API.Controllers;
using CinemaBooking.API.Contracts.ShowtimeTypes;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Infrastructure.ActivityLogs;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CinemaBooking.API.Tests;

public sealed class ShowtimeTypeContractTests
{
    [Fact]
    public void Module_RequiresAdminOrManager()
    {
        var attribute = Assert.Single(typeof(ShowtimeTypeController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal($"{Roles.Admin},{Roles.Manager}", attribute.Roles);
    }

    [Theory]
    [InlineData(nameof(ShowtimeTypeController.List), "")]
    [InlineData(nameof(ShowtimeTypeController.Get), "{id:int}")]
    [InlineData(nameof(ShowtimeTypeController.Create), "")]
    [InlineData(nameof(ShowtimeTypeController.Update), "{id:int}")]
    [InlineData(nameof(ShowtimeTypeController.Delete), "{id:int}")]
    [InlineData(nameof(ShowtimeTypeController.Preview), "preview")]
    [InlineData(nameof(ShowtimeTypeController.Generate), "generate")]
    public void RequiredEndpoint_IsExposed(string methodName, string template)
    {
        var method = typeof(ShowtimeTypeController).GetMethod(methodName)!;
        var route = Assert.Single(method.GetCustomAttributes(true).OfType<HttpMethodAttribute>());
        Assert.Equal(template, route.Template ?? string.Empty);
    }

    [Fact]
    public void Showtime_TracksNullableTemplateSource()
    {
        var property = typeof(Showtime).GetProperty(nameof(Showtime.ShowtimeTypeID));
        Assert.NotNull(property);
        Assert.Equal(typeof(int?), property!.PropertyType);
    }

    [Theory]
    [InlineData(typeof(CreateShowtimeTypeRequest), "Name")]
    [InlineData(typeof(CreateShowtimeTypeRequest), "Slots")]
    [InlineData(typeof(UpdateShowtimeTypeRequest), "Name")]
    [InlineData(typeof(UpdateShowtimeTypeRequest), "Slots")]
    public void RequestValidation_IsDefinedOnPrimaryConstructorParameter(Type type, string parameterName)
    {
        var parameter = type.GetConstructors().Single().GetParameters()
            .Single(item => item.Name == parameterName);
        Assert.NotEmpty(parameter.GetCustomAttributes(typeof(ValidationAttribute), true));
    }

    [Fact]
    public void ActivityLog_AllowsShowtimeTypeActions()
    {
        var service = new ActivityLogService(null!);
        var actions = service.GetActionTypes();
        Assert.Contains(AdminActionTypes.CreateShowtimeType, actions);
        Assert.Contains(AdminActionTypes.UpdateShowtimeType, actions);
        Assert.Contains(AdminActionTypes.DeleteShowtimeType, actions);
        Assert.Contains(AdminActionTypes.GenerateShowtimeByType, actions);
    }
}
