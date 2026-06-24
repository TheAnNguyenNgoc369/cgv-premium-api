using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Users;
using CinemaBooking.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace CinemaBooking.API.Tests;

public sealed class UserServiceAvatarErrorTests
{
    [Fact]
    public async Task UploadAvatarSucceedsWhenPreviousAvatarDeletionFailsAfterDatabaseUpdate()
    {
        var repository = new UserRepositoryFake
        {
            User = CreateUser("https://example.com/old.png", "avatars/old-public-id")
        };
        var storage = new ImageStorageServiceFake
        {
            UploadResult = new StoredImageResult("https://example.com/new.png", "avatars/new-public-id"),
            DeleteFailures =
            {
                ["avatars/old-public-id"] = new InvalidOperationException(
                    "Cloudinary failed for avatars/old-public-id")
            }
        };
        var service = CreateService(repository, storage);

        var result = await service.UploadAvatarAsync(
            repository.User.UserID,
            new MemoryStream([1, 2, 3]),
            "avatar.png",
            "image/png",
            3);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("https://example.com/new.png", repository.User.AvatarURL);
        Assert.Equal("avatars/new-public-id", repository.User.AvatarPublicId);
        Assert.Contains("avatars/old-public-id", storage.DeletedPublicIds);
        Assert.DoesNotContain("avatars/new-public-id", storage.DeletedPublicIds);
    }

    [Fact]
    public async Task UploadAvatarCleansUpNewUploadAndKeepsOldAvatarWhenDatabaseUpdateFails()
    {
        var repository = new UserRepositoryFake
        {
            User = CreateUser("https://example.com/old.png", "avatars/old-public-id"),
            ReturnNullOnUpdateAvatar = true
        };
        var storage = new ImageStorageServiceFake
        {
            UploadResult = new StoredImageResult("https://example.com/new.png", "avatars/new-public-id")
        };
        var service = CreateService(repository, storage);

        var result = await service.UploadAvatarAsync(
            repository.User.UserID,
            new MemoryStream([1, 2, 3]),
            "avatar.png",
            "image/png",
            3);

        Assert.False(result.Succeeded);
        Assert.Equal("User not found", result.ErrorMessage);
        Assert.Equal("https://example.com/old.png", repository.User.AvatarURL);
        Assert.Equal("avatars/old-public-id", repository.User.AvatarPublicId);
        Assert.Contains("avatars/new-public-id", storage.DeletedPublicIds);
        Assert.DoesNotContain("avatars/old-public-id", storage.DeletedPublicIds);
    }

    [Fact]
    public async Task DeleteAvatarReturnsStableErrorWhenStorageDeletionFails()
    {
        var repository = new UserRepositoryFake
        {
            User = CreateUser("https://example.com/avatar.png", "avatars/public-id")
        };
        var storage = new ImageStorageServiceFake
        {
            DeleteFailures =
            {
                ["avatars/public-id"] = new InvalidOperationException(
                    "Cloudinary failed for avatars/public-id")
            }
        };
        var service = CreateService(repository, storage);

        var result = await service.DeleteAvatarAsync(repository.User.UserID);

        Assert.False(result.Succeeded);
        Assert.Equal("Avatar could not be deleted. Please try again later.", result.ErrorMessage);
        Assert.DoesNotContain("Cloudinary", result.ErrorMessage);
        Assert.DoesNotContain("avatars/public-id", result.ErrorMessage);
    }

    private static UserService CreateService(
        IUserRepository repository,
        IImageStorageService storage)
    {
        return new UserService(repository, storage, NullLogger<UserService>.Instance);
    }

    private static User CreateUser(string? avatarUrl, string? avatarPublicId)
    {
        return new User
        {
            UserID = 42,
            FullName = "Avatar User",
            Email = "avatar@example.com",
            PasswordHash = "hash",
            Role = "customer",
            Status = "active",
            AvatarURL = avatarUrl,
            AvatarPublicId = avatarPublicId
        };
    }

    private sealed class ImageStorageServiceFake : IImageStorageService
    {
        public StoredImageResult UploadResult { get; init; } =
            new("https://example.com/avatar.png", "avatars/public-id");

        public Dictionary<string, Exception> DeleteFailures { get; init; } = [];

        public List<string> DeletedPublicIds { get; } = [];

        public Task<StoredImageResult> UploadImageAsync(
            Stream imageStream,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UploadResult);
        }

        public Task DeleteImageAsync(
            string publicId,
            CancellationToken cancellationToken = default)
        {
            DeletedPublicIds.Add(publicId);

            if (DeleteFailures.TryGetValue(publicId, out var exception))
            {
                throw exception;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class UserRepositoryFake : IUserRepository
    {
        public User User { get; init; } = null!;

        public bool ReturnNullOnUpdateAvatar { get; init; }

        public Task<User?> GetByIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(userId == User.UserID ? User : null);
        }

        public Task<User?> UpdateAvatarAsync(
            int userId,
            string? avatarUrl,
            string? avatarPublicId,
            CancellationToken cancellationToken = default)
        {
            if (ReturnNullOnUpdateAvatar)
            {
                return Task.FromResult<User?>(null);
            }

            if (userId != User.UserID)
            {
                return Task.FromResult<User?>(null);
            }

            User.AvatarURL = avatarUrl;
            User.AvatarPublicId = avatarPublicId;

            return Task.FromResult<User?>(User);
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<User?> UpdateProfileAsync(
            int userId,
            string fullName,
            string? phone,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddUserWithWalletAsync(User user, Wallet wallet, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddUserWithWalletAndVerificationTokenAsync(
            User user,
            Wallet wallet,
            EmailVerificationToken verificationToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddEmailVerificationTokenAsync(
            EmailVerificationToken verificationToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(
            string token,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EmailVerificationToken?> GetLatestEmailVerificationTokenAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddPasswordResetTokenAsync(
            PasswordResetToken resetToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PasswordResetToken?> GetLatestPasswordResetTokenAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ReplaceUnusedPasswordResetTokensAsync(
            int userId,
            PasswordResetToken resetToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ReplaceUnverifiedEmailVerificationTokensAsync(
            int userId,
            EmailVerificationToken verificationToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeletePasswordResetTokenAsync(string token, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryResetPasswordAsync(
            string token,
            string passwordHash,
            DateTime resetAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryIncrementTokenVersionAsync(
            int userId,
            int expectedTokenVersion,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryUpdatePasswordHashAsync(
            int userId,
            string expectedPasswordHash,
            string newPasswordHash,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
