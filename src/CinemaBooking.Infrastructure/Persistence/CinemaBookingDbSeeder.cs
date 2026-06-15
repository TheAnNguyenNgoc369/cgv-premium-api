using CinemaBooking.Application.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBooking.Infrastructure.Persistence;

public static class CinemaBookingDbSeeder
{
    public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
    {
        var authService = serviceProvider.GetRequiredService<IAuthService>();

        var users = new[]
        {
            new { Name = "Admin User", Email = "admin@cinema.com", Phone = "0900000001", Role = "admin" },
            new { Name = "Staff 1 User", Email = "staff1@cinema.com", Phone = "0900000023", Role = "staff" },
            new { Name = "Staff 2 User", Email = "staff2@cinema.com", Phone = "0900000024", Role = "staff" },
            new { Name = "Customer One", Email = "c1@cinema.com", Phone = "0900000003", Role = "customer" },
            new { Name = "Customer Two", Email = "c2@cinema.com", Phone = "0900000004", Role = "customer" },
            new { Name = "Customer Three", Email = "c3@cinema.com", Phone = "0900000005", Role = "customer" }
        };

        foreach (var u in users)
        {
            try
            {
                await authService.RegisterAsync(
                    u.Name,
                    u.Email,
                    u.Phone,
                    "Password@123",
                    CancellationToken.None
                );

            }
            catch
            {
            }
        }
    }
}