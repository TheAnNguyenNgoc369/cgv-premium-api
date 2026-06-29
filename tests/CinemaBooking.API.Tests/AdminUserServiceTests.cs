using CinemaBooking.Application.AdminUsers;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.Extensions.Logging.Abstractions;

namespace CinemaBooking.API.Tests;

public sealed class AdminUserServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCustomer_HashesPasswordCreatesWalletAndLog()
    {
        var repository = new StubAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            1,
            new AdminUserCreateCommand(
                "New User", "new@example.com", "0901234567", "Password@123",
                Roles.Customer, null, null),
            "127.0.0.1");

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.AddedUser);
        Assert.NotEqual("Password@123", repository.AddedUser.PasswordHash);
        Assert.True(PasswordHasher.Verify(
            "Password@123", repository.AddedUser.PasswordHash, out _));
        Assert.Equal(UserStatuses.Active, repository.AddedUser.Status);
        Assert.NotNull(repository.AddedUser.EmailVerifiedAt);
        Assert.Equal(0m, repository.AddedWallet!.Balance);
        Assert.Equal(AdminActionTypes.CreateUser, repository.AddedLog!.ActionType);
    }

    [Fact]
    public async Task DeleteAsync_UserHasBookings_DeactivatesInsteadOfDeleting()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User { UserID = 2, Role = Roles.Customer, Status = UserStatuses.Active },
            HasBookingHistory = true
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(1, 2, null);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.Deactivated);
        Assert.False(result.Value.PhysicallyDeleted);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(UserStatuses.Inactive, repository.ChangedStatus);
        Assert.Equal(AdminActionTypes.DeactivateUser, repository.LastLog!.ActionType);
    }

    [Fact]
    public async Task ChangeStatusAsync_AdminLocksSelf_ReturnsForbidden()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User { UserID = 1, Role = Roles.Admin, Status = UserStatuses.Active }
        };
        var service = CreateService(repository);

        var result = await service.ChangeStatusAsync(1, 1, UserStatuses.Locked, null);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminUserErrorType.Forbidden, result.ErrorType);
        Assert.Null(repository.ChangedStatus);
    }

    [Fact]
    public async Task DeleteAsync_ForeignKeyBlocksDelete_DeactivatesUser()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User { UserID = 2, Role = Roles.Staff, Status = UserStatuses.Active },
            TryDeleteResult = false
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(1, 2, null);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.Deactivated);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(UserStatuses.Inactive, repository.ChangedStatus);
        Assert.Equal(AdminActionTypes.DeactivateUser, repository.LastLog!.ActionType);
    }

    private static AdminUserService CreateService(StubAdminUserRepository repository) =>
        new(repository, new StubImageStorageService(), NullLogger<AdminUserService>.Instance);

    private sealed class StubImageStorageService : IImageStorageService
    {
        public Task<StoredImageResult> UploadImageAsync(
            Stream imageStream, string fileName, string folder,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredImageResult("https://example.com/avatar", "avatar-id"));

        public Task DeleteImageAsync(
            string publicId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubAdminUserRepository : IAdminUserRepository
    {
        public User? User { get; set; }
        public bool HasBookingHistory { get; set; }
        public bool TryDeleteResult { get; set; } = true;
        public User? AddedUser { get; private set; }
        public Wallet? AddedWallet { get; private set; }
        public AdminActionLog? AddedLog { get; private set; }
        public AdminActionLog? LastLog { get; private set; }
        public string? ChangedStatus { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPageAsync(
            string? search, string? role, string? status, int page, int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<User>)Array.Empty<User>(), 0));

        public Task<User?> GetByIdAsync(
            int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(User?.UserID == userId ? User : null);

        public Task<bool> EmailExistsAsync(
            string email, int? excludingUserId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> CinemaExistsAsync(
            int cinemaId, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> HasBookingHistoryAsync(
            int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(HasBookingHistory);

        public Task AddAsync(
            User user, Wallet wallet, AdminActionLog actionLog,
            CancellationToken cancellationToken = default)
        {
            user.UserID = 2;
            AddedUser = user;
            AddedWallet = wallet;
            AddedLog = actionLog;
            return Task.CompletedTask;
        }

        public Task<User?> UpdateAsync(
            int userId, string fullName, string email, string phone, int? cinemaId,
            AdminActionLog actionLog, CancellationToken cancellationToken = default) =>
            Task.FromResult(User);

        public Task<User?> ChangeRoleAsync(
            int userId, string role, int? cinemaId, AdminActionLog actionLog,
            CancellationToken cancellationToken = default) => Task.FromResult(User);

        public Task<User?> ChangeStatusAsync(
            int userId, string status, AdminActionLog actionLog,
            CancellationToken cancellationToken = default)
        {
            ChangedStatus = status;
            LastLog = actionLog;
            if (User is not null) User.Status = status;
            return Task.FromResult(User);
        }

        public Task<User?> ResetPasswordAsync(
            int userId, string passwordHash, AdminActionLog actionLog,
            CancellationToken cancellationToken = default) => Task.FromResult(User);

        public Task<User?> UpdateAvatarAsync(
            int userId, string? avatarUrl, string? avatarPublicId, AdminActionLog actionLog,
            CancellationToken cancellationToken = default) => Task.FromResult(User);

        public Task<bool> TryDeleteAsync(
            int userId, AdminActionLog actionLog,
            CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;
            return Task.FromResult(TryDeleteResult);
        }
    }
}
