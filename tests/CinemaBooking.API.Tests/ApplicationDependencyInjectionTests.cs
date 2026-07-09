using CinemaBooking.Application;
using CinemaBooking.Application.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBooking.API.Tests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplicationServices_RegistersManagerCinemaScopeService()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IManagerCinemaScopeService)
            && descriptor.ImplementationType == typeof(ManagerCinemaScopeService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
