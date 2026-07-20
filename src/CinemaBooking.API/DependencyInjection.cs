using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CinemaBooking.API.Configuration;
using CinemaBooking.API.OpenApi;
using CinemaBooking.API.Services;
using CinemaBooking.API.Serialization;
using CinemaBooking.Application;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Configuration;
using CinemaBooking.Application.Payments.PayOS;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
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
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new VietnamDateTimeJsonConverter());
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(allowIntegerValues: false));
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errorEntry = context.ModelState
                        .Where(kvp => kvp.Value?.Errors.Count > 0)
                        .Select(kvp => new { Key = kvp.Key, Error = kvp.Value?.Errors.First().ErrorMessage })
                        .FirstOrDefault();

                    string GetFieldName(string key)
                    {
                        var lastDot = key.LastIndexOf('.');
                        return lastDot >= 0 ? key[(lastDot + 1)..] : key;
                    }

                    if (errorEntry?.Key is not null)
                    {
                        var fieldName = GetFieldName(errorEntry.Key);
                        var errorText = errorEntry.Error ?? string.Empty;
                        var isEnumError = errorText.Contains("could not be converted", StringComparison.OrdinalIgnoreCase);
                        if (isEnumError)
                        {
                            return new BadRequestObjectResult(new
                            {
                                success = false,
                                message = $"{fieldName} has an invalid enum value."
                            });
                        }

                        if (context.HttpContext?.Request.Path.StartsWithSegments("/api/showtime-types", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            var message = string.IsNullOrWhiteSpace(errorText)
                                ? $"{char.ToLowerInvariant(fieldName[0])}{fieldName[1..]} is invalid."
                                : errorText;
                            return new BadRequestObjectResult(new { success = false, message });
                        }

                        if (context.HttpContext?.Request.Path.StartsWithSegments("/api/v1/reports", StringComparison.OrdinalIgnoreCase) == true
                            && (fieldName == "startDate" || fieldName == "endDate"))
                        {
                            return new BadRequestObjectResult(new { success = false, message = $"{fieldName} is required. Expected format: yyyy-MM-dd." });
                        }

                        if (context.HttpContext?.Request.Path.StartsWithSegments("/api/admin/email-logs", StringComparison.OrdinalIgnoreCase) == true
                            && (fieldName == "fromDate" || fieldName == "toDate"))
                        {
                            return new BadRequestObjectResult(new { success = false, message = $"{fieldName} has an invalid format. Expected format: yyyy-MM-dd." });
                        }

                        if (context.HttpContext?.Request.Path.StartsWithSegments("/api/vouchers", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            var isRequiredError = errorText.Contains("required", StringComparison.OrdinalIgnoreCase)
                                || errorText.Contains("cannot be null", StringComparison.OrdinalIgnoreCase)
                                || errorText.Contains("required value", StringComparison.OrdinalIgnoreCase);

                            if (isRequiredError)
                            {
                                return new BadRequestObjectResult(new { success = false, message = $"{fieldName} is required and cannot be null." });
                            }

                            var detail = fieldName switch
                            {
                                "category" => "Allowed values: Discount, Combo, Cashback.",
                                "discountType" => "Allowed values: percent, fixed.",
                                "discountValue" => "Must be between 0-100 for percent or >= 0 for fixed.",
                                "validFrom" or "validUntil" => "Use ISO 8601 format (e.g. 2026-07-01T00:00:00+07:00).",
                                "maxUses" => "maxUses must be greater than 0.",
                                _ => "Please check the data constraints."
                            };

                            return new BadRequestObjectResult(new { success = false, message = $"{fieldName} is invalid. {detail}" });
                        }
                    }

                    return new BadRequestObjectResult(new { success = false, message = "Invalid request data. Please check the input fields." });
                };
            });

        //Add Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<AuthRequestSchemaFilter>();
            options.SchemaFilter<ShowtimeRequestSchemaFilter>();
            options.SchemaFilter<VoucherRequestSchemaFilter>();

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
        services.AddScoped<ITokenRevocationService, DatabaseTokenRevocationService>();
        services.AddScoped<IReviewRewardSettingsService, ReviewRewardSettingsService>();
        services.AddSingleton<IAuthRequestRateLimiter, AuthRequestRateLimiter>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                AuthRateLimitPolicyNames.Login,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(httpContext, "login"),
                    _ => CreateFixedWindowOptions(10)));
            options.AddPolicy(
                AuthRateLimitPolicyNames.Register,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(httpContext, "register"),
                    _ => CreateFixedWindowOptions(5)));
            options.AddPolicy(
                AuthRateLimitPolicyNames.EmailAction,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(httpContext, "email-action"),
                    _ => CreateFixedWindowOptions(5)));
            options.AddPolicy(
                AuthRateLimitPolicyNames.Verify,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(httpContext, "verify"),
                    _ => CreateFixedWindowOptions(10)));
            options.AddPolicy(
                AiRateLimitPolicyNames.Chat,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    GetAiPartitionKey(httpContext),
                    _ => CreateFixedWindowOptions(20)));
        });

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

        services.AddOptions<FrontendSettings>()
            .Bind(configuration.GetRequiredSection(FrontendSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(settings => Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp),
                "Frontend:BaseUrl must be an absolute HTTP or HTTPS URL.")
            .Validate(settings => settings.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)),
                "Frontend:AllowedOrigins entries must be absolute HTTP or HTTPS URLs.")
            .ValidateOnStart();

        services.AddOptions<PayOSSettings>()
            .Bind(configuration.GetRequiredSection(PayOSSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(settings => IsOptionalHttpUrl(settings.ReturnUrl),
                "PayOS:ReturnUrl must be empty or an absolute HTTP or HTTPS URL.")
            .Validate(settings => IsOptionalHttpUrl(settings.CancelUrl),
                "PayOS:CancelUrl must be empty or an absolute HTTP or HTTPS URL.")
            .Validate(settings => !settings.ConfirmWebhookOnStartup || IsPublicHttpsUrl(settings.WebhookUrl),
                "PayOS:WebhookUrl must be a public HTTPS URL when PayOS:ConfirmWebhookOnStartup is enabled.")
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
                        var rawToken = context.SecurityToken switch
                        {
                            JsonWebToken jsonWebToken => jsonWebToken.EncodedToken,
                            JwtSecurityToken jwtToken => jwtToken.RawData,
                            _ => null
                        };

                        if (string.IsNullOrWhiteSpace(rawToken)
                            || await revocationService.IsRevokedAsync(
                                rawToken,
                                context.HttpContext.RequestAborted))
                        {
                            context.Fail("Token has been revoked.");
                        }
                    },

                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuthentication");
                        logger.LogWarning(
                            context.Exception,
                            "JWT authentication failed for request {Path}.",
                            context.HttpContext.Request.Path);

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Roles.Customer, policy => policy.RequireRole(Roles.Customer));
            options.AddPolicy(Roles.Staff, policy => policy.RequireRole(Roles.Staff));
            options.AddPolicy(Roles.Manager, policy => policy.RequireRole(Roles.Manager));
            options.AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin));
        });

        return services;
    }

    private static string GetClientPartitionKey(HttpContext httpContext, string action)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"{action}:{remoteIp}";
    }

    private static string GetAiPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"ai-chat:user:{userId}";
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ai-chat:ip:{remoteIp}";
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(int permitLimit)
    {
        return new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        };
    }

    private static bool IsOptionalHttpUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
    }

    private static bool IsPublicHttpsUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && !uri.IsLoopback;
    }
}
