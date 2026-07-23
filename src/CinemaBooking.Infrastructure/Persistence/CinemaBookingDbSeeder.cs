using System.Security.Cryptography;
using System.Text;
using CinemaBooking.Application.Common.Security;
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

        var now = DateTime.UtcNow;
        var users = new[]
        {
            new User
            {
                FullName = "Admin Cinema",
                Email = "admin@cinema.com",
                Phone = "0900000001",
                PasswordHash = PasswordHasher.Hash("Password@123"),
                Role = "admin",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            
            new User
            {
                FullName = "Manager Cinema",
                Email = "manager@cinema.com",
                Phone = "0900000003",
                PasswordHash = PasswordHasher.Hash("Password@123"),
                Role = "manager",
                CinemaID = 1,
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },

            new User
            {
                FullName = "Staff 1",
                Email = "staff1@cinema.com",
                Phone = "0900000023",
                PasswordHash = PasswordHasher.Hash("Password@123"),
                Role = "staff",
                CinemaID = 1,
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Staff 2",
                Email = "staff2@cinema.com",
                Phone = "0900000024",
                PasswordHash = PasswordHasher.Hash("Password@123"),
                Role = "staff",
                CinemaID = 1,
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Customer 1",
                Email = "c1@cinema.com",
                Phone = "0900000031",
                PasswordHash = PasswordHasher.Hash("Password@123"),
                Role = "customer",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Customer 2",
                Email = "c2@cinema.com",
                Phone = "0900000004",
                PasswordHash = PasswordHasher.Hash("Password@123"),
                Role = "customer",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                FullName = "Customer 3",
                Email = "c3@cinema.com",
                Phone = "0900000005",
                PasswordHash = PasswordHasher.Hash("Password@123"),
                Role = "customer",
                Status = "active",
                EmailVerifiedAt = now,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var seedEmails = users.Select(user => user.Email).ToArray();
        var existingEmails = await dbContext.Users
            .Where(user => seedEmails.Contains(user.Email))
            .Select(user => user.Email)
            .ToListAsync(cancellationToken);

        var existingEmailSet = existingEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);
        await dbContext.Users
            .Where(user => (user.Email == "manager@cinema.com"
                    || user.Email == "staff1@cinema.com"
                    || user.Email == "staff2@cinema.com")
                && user.CinemaID != 1)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(user => user.CinemaID, 1),
                cancellationToken);

        var missingUsers = users
            .Where(user => !existingEmailSet.Contains(user.Email))
            .ToArray();

        if (missingUsers.Length == 0)
        {
            return;
        }

        dbContext.Users.AddRange(missingUsers);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Create wallets only for accounts restored by this seed run.
        var wallets = missingUsers.Select(user => new Wallet
        {
            UserID = user.UserID,
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

}
