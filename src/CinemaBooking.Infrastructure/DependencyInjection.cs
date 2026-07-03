using System.Reflection;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Infrastructure.Configuration;
using CinemaBooking.Infrastructure.Email;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Infrastructure.Storage;
using CinemaBooking.Infrastructure.BackgroundJobs;
using CinemaBooking.Application.Payments.PayOS;
using CinemaBooking.Infrastructure.Payments.PayOS;
using CinemaBooking.Application.Reports;
using CinemaBooking.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CinemaBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<CinemaBookingDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(CinemaBookingDbContext).Assembly.FullName)));

        var emailSection = configuration.GetRequiredSection(EmailSettings.SectionName);
        var emailSettings = new EmailSettings
        {
            Host = emailSection[nameof(EmailSettings.Host)] ?? string.Empty,
            Port = int.TryParse(emailSection[nameof(EmailSettings.Port)], out var port)
                ? port
                : 587,
            EnableSsl = !bool.TryParse(emailSection[nameof(EmailSettings.EnableSsl)], out var enableSsl)
                || enableSsl,
            FromAddress = emailSection[nameof(EmailSettings.FromAddress)] ?? string.Empty,
            FromName = emailSection[nameof(EmailSettings.FromName)] ?? string.Empty,
            Username = emailSection[nameof(EmailSettings.Username)],
            Password = emailSection[nameof(EmailSettings.Password)]
        };

        services.AddSingleton(Options.Create(emailSettings));

        var cloudinarySection = configuration.GetSection(CloudinarySettings.SectionName);
        var cloudinarySettings = new CloudinarySettings
        {
            CloudName = cloudinarySection[nameof(CloudinarySettings.CloudName)] ?? string.Empty,
            ApiKey = cloudinarySection[nameof(CloudinarySettings.ApiKey)] ?? string.Empty,
            ApiSecret = cloudinarySection[nameof(CloudinarySettings.ApiSecret)] ?? string.Empty
        };

        services.AddSingleton(Options.Create(cloudinarySettings));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
        services.AddScoped<IPayOSService, PayOSService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddHostedService<SeatHoldExpirationJob>();
        services.AddHostedService<ShowtimeCompletionJob>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScopedByConvention(typeof(DependencyInjection).Assembly, "Repository");

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
