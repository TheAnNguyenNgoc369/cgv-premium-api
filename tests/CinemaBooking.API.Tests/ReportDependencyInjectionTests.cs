using CinemaBooking.Application.Reports;
using CinemaBooking.Infrastructure;
using CinemaBooking.Infrastructure.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBooking.API.Tests;

public sealed class ReportDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersReportService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;Trusted_Connection=True",
            ["Email:Host"] = "localhost"
        }).Build();

        services.AddInfrastructureServices(configuration);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IReportService)
            && descriptor.ImplementationType == typeof(ReportService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
