using System.Security.Cryptography;
using System.Text;
using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBooking.Infrastructure.Persistence;

public static class CinemaBookingDbSeeder
{
    private const string PasswordHashAlgorithm = "pbkdf2-sha256";
    private const string PasswordHashVersion = "v1";
    private const int PasswordHashIterations = 700_000;
    private const int PasswordSaltSize = 16;
    private const int PasswordHashSize = 32;

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
                PasswordHash = HashPassword("Password@123"),
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
                PasswordHash = HashPassword("Password@123"),
                Role = "manager",
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
                PasswordHash = HashPassword("Password@123"),
                Role = "staff",
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
                PasswordHash = HashPassword("Password@123"),
                Role = "staff",
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
                Phone = "0900000003",
                PasswordHash = HashPassword("Password@123"),
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
                PasswordHash = HashPassword("Password@123"),
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
                PasswordHash = HashPassword("Password@123"),
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

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(PasswordSaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            PasswordHashIterations,
            HashAlgorithmName.SHA256,
            PasswordHashSize);

        return $"${PasswordHashAlgorithm}${PasswordHashVersion}${PasswordHashIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
