using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CinemaBooking.Application.Authentication;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class AuthServiceLoginTests
{
    [Fact]
    public async Task LoginWithLegacySha256HashSucceedsAndUpgradesHash()
    {
        const string password = "Password@123";
        var state = new UserState
        {
            User = CreateUser(HashLegacyPassword(password))
        };
        var service = CreateService(state);

        var result = await service.LoginAsync(state.User.Email, password);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.StartsWith("$pbkdf2-sha256$v1$", state.User.PasswordHash);
    }

    [Fact]
    public async Task LoginWithWrongPasswordDoesNotUpgradeLegacyHash()
    {
        var legacyHash = HashLegacyPassword("Password@123");
        var state = new UserState
        {
            User = CreateUser(legacyHash)
        };
        var service = CreateService(state);

        var result = await service.LoginAsync(state.User.Email, "wrong-password");

        Assert.False(result.Succeeded);
        Assert.Equal(legacyHash, state.User.PasswordHash);
    }

    private static AuthService CreateService(UserState state)
    {
        var repository = DispatchProxy.Create<IUserRepository, UserRepositoryProxy>();
        ((UserRepositoryProxy)(object)repository).State = state;
        return new AuthService(repository, new UnusedEmailSender());
    }

    private static User CreateUser(string passwordHash)
    {
        return new User
        {
            UserID = 42,
            FullName = "Legacy User",
            Email = "legacy@example.com",
            PasswordHash = passwordHash,
            Role = "customer",
            Status = "active",
            EmailVerifiedAt = DateTime.UtcNow
        };
    }

    private static string HashLegacyPassword(string password)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)))
            .ToLowerInvariant();
    }

    private sealed class UserState
    {
        public User User { get; init; } = null!;
    }

    private class UserRepositoryProxy : DispatchProxy
    {
        public UserState State { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                nameof(IUserRepository.GetByEmailAsync) => GetByEmailAsync((string)args![0]!),
                nameof(IUserRepository.TryUpdatePasswordHashAsync) => TryUpdatePasswordHashAsync(
                    (int)args![0]!,
                    (string)args[1]!,
                    (string)args[2]!),
                _ => throw new NotSupportedException(targetMethod?.Name)
            };
        }

        private Task<User?> GetByEmailAsync(string email)
        {
            User? user = string.Equals(State.User.Email, email, StringComparison.Ordinal)
                ? State.User
                : null;
            return Task.FromResult(user);
        }

        private Task<bool> TryUpdatePasswordHashAsync(
            int userId,
            string expectedPasswordHash,
            string newPasswordHash)
        {
            if (State.User.UserID != userId || State.User.PasswordHash != expectedPasswordHash)
            {
                return Task.FromResult(false);
            }

            State.User.PasswordHash = newPasswordHash;
            return Task.FromResult(true);
        }
    }

    private sealed class UnusedEmailSender : IEmailSender
    {
        public Task<bool> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
