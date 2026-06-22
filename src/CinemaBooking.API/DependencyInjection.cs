using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CinemaBooking.API.Configuration;
using CinemaBooking.API.Services;
using CinemaBooking.Application;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

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
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document, null),
                    []
                }
            });
        });


        services.AddApplicationServices();
        services.AddScoped<JwtTokenService>();
        services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();

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
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var revocationService = context.HttpContext.RequestServices
                            .GetRequiredService<ITokenRevocationService>();
                        var rawToken = context.SecurityToken is JwtSecurityToken jwtToken
                            ? jwtToken.RawData
                            : null;

                        if (!string.IsNullOrWhiteSpace(rawToken)
                            && revocationService.IsRevoked(rawToken))
                        {
                            context.Fail("Token has been revoked.");
                            return;
                        }

                        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var tokenVersionValue = context.Principal?.FindFirstValue(JwtTokenService.TokenVersionClaim);

                        if (!int.TryParse(userIdValue, out var userId)
                            || !int.TryParse(tokenVersionValue, out var tokenVersion))
                        {
                            context.Fail("Token authentication state is invalid.");
                            return;
                        }

                        var userRepository = context.HttpContext.RequestServices
                            .GetRequiredService<IUserRepository>();
                        var user = await userRepository.GetByIdAsync(
                            userId,
                            context.HttpContext.RequestAborted);

                        if (user is null
                            || !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase)
                            || user.TokenVersion != tokenVersion)
                        {
                            context.Fail("Token has been invalidated.");
                        }
                    },

                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("JWT ERROR:");
                        Console.WriteLine(context.Exception.Message);

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Roles.Customer, policy => policy.RequireRole(Roles.Customer));
            options.AddPolicy(Roles.Staff, policy => policy.RequireRole(Roles.Staff));
            options.AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin));
        });

        return services;
    }
}
