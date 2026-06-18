using System.Security.Cryptography;
using System.Text;
using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBooking.Infrastructure.Persistence;

public static class CinemaBookingDbSeeder
{
    public static async Task SeedUsersAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var dbContext = serviceProvider.GetRequiredService<CinemaBookingDbContext>();

        await PrepareDatabaseAsync(dbContext, cancellationToken);

        // Only seed if no users exist yet
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var passwordHash = HashPassword("Password@123");

        var users = new[]
        {
            new User
            {
                FullName = "Admin User",
                Email = "admin@cinema.com",
                Phone = "0900000001",
                PasswordHash = passwordHash,
                Role = "admin",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            
            new User
            {
                FullName = "Manager User",
                Email = "manager@cinema.com",
                Phone = "0900000003",
                PasswordHash = passwordHash,
                Role = "manager",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },

            new User
            {
                FullName = "Staff 1 User",
                Email = "staff1@cinema.com",
                Phone = "0900000023",
                PasswordHash = passwordHash,
                Role = "staff",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Staff 2 User",
                Email = "staff2@cinema.com",
                Phone = "0900000024",
                PasswordHash = passwordHash,
                Role = "staff",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Customer One",
                Email = "c1@cinema.com",
                Phone = "0900000003",
                PasswordHash = passwordHash,
                Role = "customer",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Customer Two",
                Email = "c2@cinema.com",
                Phone = "0900000004",
                PasswordHash = passwordHash,
                Role = "customer",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Customer Three",
                Email = "c3@cinema.com",
                Phone = "0900000005",
                PasswordHash = passwordHash,
                Role = "customer",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Create wallets for seeded users
        var wallets = users.Select(u => new Wallet
        {
            UserID = u.UserID,
            Balance = 0m
        });

        dbContext.Wallets.AddRange(wallets);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task PrepareDatabaseAsync(
        CinemaBookingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
