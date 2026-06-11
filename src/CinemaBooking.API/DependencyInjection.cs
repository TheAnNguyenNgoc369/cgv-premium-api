using System.Text;
using CinemaBooking.API.Configuration;
using CinemaBooking.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

namespace CinemaBooking.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers();

        //Add Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddApplicationServices();

        if (environment.IsDevelopment())
        {
            var dataProtectionKeysPath = Path.Combine(
                Path.GetTempPath(),
                "CinemaBooking.API",
                "DataProtectionKeys");

            Directory.CreateDirectory(dataProtectionKeysPath);

            var dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName("CinemaBooking.API")
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

            if (OperatingSystem.IsWindows())
            {
                dataProtectionBuilder.ProtectKeysWithDpapi();
            }
        }

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetRequiredSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetRequiredSection(EmailSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = configuration
            .GetRequiredSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt configuration section is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
