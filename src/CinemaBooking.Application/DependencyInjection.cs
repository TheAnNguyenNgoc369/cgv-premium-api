using System.Reflection;
using CinemaBooking.Application.Features.AI;
using CinemaBooking.Application.Features.AI.DTOs;
using CinemaBooking.Application.Vouchers.RuleEngine;
using CinemaBooking.Application.Vouchers.RuleEngine.Metadata;
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

        // AI services
        services.AddScoped<IIntentRouter, IntentRouter>();
        services.AddSingleton<IConversationStore, ConversationStore>();
        services.AddSingleton<IPromptBuilder, PromptBuilder>();

        // Registry data is static — safe to share a single instance process-wide.
        services.AddSingleton<IVoucherRuleMetadataProvider, VoucherRuleMetadataProvider>();

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
