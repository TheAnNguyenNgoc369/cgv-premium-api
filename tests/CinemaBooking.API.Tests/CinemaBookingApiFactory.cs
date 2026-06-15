using System.Text;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CinemaBooking.API.Tests;

internal sealed class CinemaBookingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"CinemaBookingTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=CinemaBookingTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "CinemaBooking.API",
                ["Jwt:Audience"] = "CinemaBooking.Client",
                ["Jwt:SigningKey"] = "DevelopmentOnlyJwtSigningKeyChangeMe1234567890",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Email:Host"] = "localhost",
                ["Email:Port"] = "25",
                ["Email:EnableSsl"] = "false",
                ["Email:FromAddress"] = "no-reply@example.com",
                ["Email:FromName"] = "Cinema Booking Tests"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CinemaBookingDbContext>();
            services.RemoveAll<DbContextOptions<CinemaBookingDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CinemaBookingDbContext>>();

            services.AddDbContext<CinemaBookingDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.ValidIssuer = "CinemaBooking.API";
                    options.TokenValidationParameters.ValidAudience = "CinemaBooking.Client";
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes("DevelopmentOnlyJwtSigningKeyChangeMe1234567890"));
                });
        });
    }
}
