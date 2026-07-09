using CinemaBooking.Application.Authentication;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.API.Tests;

public sealed class AuthAccountStatusTests
{
    [Fact]
    public async Task RegisterAsync_ExistingInactiveEmail_ReturnsEmailInUse()
    {
        var service = new AuthService(
            new StubUserRepository { EmailExists = true }, new StubEmailSender(), new StubAuthEmailService());

        var result = await service.RegisterAsync(
            "Existing User", "existing@example.com", "0901234567", "Password@123");

        Assert.False(result.Succeeded);
        Assert.Equal("Email is already in use.", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_ExistingPhone_ReturnsPhoneInUse()
    {
        var service = new AuthService(
            new StubUserRepository { PhoneExists = true }, new StubEmailSender(), new StubAuthEmailService());

        var result = await service.RegisterAsync(
            "Existing Phone", "phone@example.com", "0901234567", "Password@123");

        Assert.False(result.Succeeded);
        Assert.Equal("Phone is already in use.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsContactAdministratorMessage()
    {
        const string password = "Password@123";
        var service = new AuthService(new StubUserRepository
        {
            User = new User
            {
                UserID = 2,
                Email = "inactive@example.com",
                PasswordHash = PasswordHasher.Hash(password),
                Status = UserStatuses.Inactive,
                EmailVerifiedAt = DateTime.UtcNow
            }
        }, new StubEmailSender(), new StubAuthEmailService());

        var result = await service.LoginAsync("inactive@example.com", password);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Your account is currently inactive. Please contact our Administrator for assistance",
            result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_VerifiedUserStillMarkedUnverified_ActivatesAndAllowsLogin()
    {
        const string password = "Password@123";
        var user = new User
        {
            UserID = 4,
            Email = "verified@example.com",
            PasswordHash = PasswordHasher.Hash(password),
            Status = UserStatuses.Unverified,
            EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var repository = new StubUserRepository { User = user };
        var service = new AuthService(repository, new StubEmailSender(), new StubAuthEmailService());

        var result = await service.LoginAsync(user.Email, password);

        Assert.True(result.Succeeded);
        Assert.Same(user, result.User);
        Assert.Equal(UserStatuses.Active, user.Status);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task VerifyEmailAsync_AdminMarkedUserUnverified_ActivatesAccount()
    {
        var user = new User
        {
            UserID = 3,
            Email = "unverified@example.com",
            Status = UserStatuses.Unverified
        };
        var token = new EmailVerificationToken
        {
            Token = "verification-code",
            UserID = user.UserID,
            User = user,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        var repository = new StubUserRepository { VerificationToken = token };
        var service = new AuthService(repository, new StubEmailSender(), new StubAuthEmailService());

        var result = await service.VerifyEmailAsync(token.Token);

        Assert.True(result.Succeeded);
        Assert.Equal(UserStatuses.Active, user.Status);
        Assert.NotNull(user.EmailVerifiedAt);
        Assert.NotNull(token.VerifiedAt);
        Assert.True(repository.SaveChangesCalled);
    }

    private sealed class StubEmailSender : IEmailSender
    {
        public Task<bool> SendAsync(string toEmail, string subject, string htmlBody,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class StubAuthEmailService : IAuthEmailService
    {
        public Task QueueVerificationAsync(int userId, string email, string subject, string htmlBody,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task QueuePasswordResetAsync(int userId, string email, string subject, string htmlBody,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public bool EmailExists { get; init; }
        public bool PhoneExists { get; init; }
        public User? User { get; init; }
        public EmailVerificationToken? VerificationToken { get; init; }
        public bool SaveChangesCalled { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(EmailExists);
        public Task<bool> PhoneExistsAsync(string phone, int? excludingUserId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(PhoneExists);
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(User);
        public Task<bool> TryUpdatePasswordHashAsync(int userId, string expectedPasswordHash,
            string newPasswordHash, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<User?> GetProfileByIdAsync(int userId, CancellationToken cancellationToken = default) => Unsupported<User?>();
        public Task<User?> LookupCustomerAsync(string? email, string? phone,
            CancellationToken cancellationToken = default) => Unsupported<User?>();
        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => Unsupported<User?>();
        public Task<User?> UpdateProfileAsync(int userId, string fullName, string? phone,
            CancellationToken cancellationToken = default) => Unsupported<User?>();
        public Task<User?> UpdateAvatarAsync(int userId, string? avatarUrl, string? avatarPublicId,
            CancellationToken cancellationToken = default) => Unsupported<User?>();
        public Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default) => Unsupported<Wallet?>();
        public Task<bool> DeleteAsync(int userId, CancellationToken cancellationToken = default) => Unsupported<bool>();
        public Task AddUserWithWalletAsync(User user, Wallet wallet, CancellationToken cancellationToken = default) => Unsupported();
        public Task AddUserWithWalletAndVerificationTokenAsync(User user, Wallet wallet,
            EmailVerificationToken verificationToken, CancellationToken cancellationToken = default) => Unsupported();
        public Task AddEmailVerificationTokenAsync(EmailVerificationToken verificationToken,
            CancellationToken cancellationToken = default) => Unsupported();
        public Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string token,
            CancellationToken cancellationToken = default) => Task.FromResult(VerificationToken);
        public Task<EmailVerificationToken?> GetLatestEmailVerificationTokenAsync(int userId,
            CancellationToken cancellationToken = default) => Unsupported<EmailVerificationToken?>();
        public Task AddPasswordResetTokenAsync(PasswordResetToken resetToken,
            CancellationToken cancellationToken = default) => Unsupported();
        public Task<PasswordResetToken?> GetLatestPasswordResetTokenAsync(int userId,
            CancellationToken cancellationToken = default) => Unsupported<PasswordResetToken?>();
        public Task ReplaceUnusedPasswordResetTokensAsync(int userId, PasswordResetToken resetToken,
            CancellationToken cancellationToken = default) => Unsupported();
        public Task ReplaceUnverifiedEmailVerificationTokensAsync(int userId,
            EmailVerificationToken verificationToken, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteEmailVerificationTokenAsync(string token,
            CancellationToken cancellationToken = default) => Unsupported();
        public Task DeletePasswordResetTokenAsync(string token,
            CancellationToken cancellationToken = default) => Unsupported();
        public Task<bool> TryResetPasswordAsync(string token, string passwordHash, DateTime resetAt,
            CancellationToken cancellationToken = default) => Unsupported<bool>();
        public Task<bool> TryIncrementTokenVersionAsync(int userId, int expectedTokenVersion,
            CancellationToken cancellationToken = default) => Unsupported<bool>();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        private static Task Unsupported() => throw new NotSupportedException();
        private static Task<T> Unsupported<T>() => throw new NotSupportedException();
    }
}
