using System.Reflection;
using CinemaBooking.Application.Vouchers.RuleEngine;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddAutoMapper(_ => { }, assembly);
        services.AddScopedByConvention(assembly, "Service");
        services.AddScoped<IVoucherRuleEngine, VoucherRuleEngine>();

        return services;
    }

    private static IServiceCollection AddScopedByConvention(
        this IServiceCollection services,
        Assembly assembly,
        string implementationSuffix)
    {
        var implementationTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && type.Name.EndsWith(implementationSuffix, StringComparison.Ordinal));

        foreach (var implementationType in implementationTypes)
        {
            var serviceTypes = implementationType.GetInterfaces()
                .Where(type => type.Name == $"I{implementationType.Name}");

            foreach (var serviceType in serviceTypes)
            {
                if (services.Any(descriptor =>
                        descriptor.ServiceType == serviceType
                        && descriptor.ImplementationType == implementationType))
                {
                    continue;
                }

                services.AddScoped(serviceType, implementationType);
            }
        }

        return services;
    }
}
