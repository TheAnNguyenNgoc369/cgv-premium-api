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
    public async Task CreateAsync_ValidStaff_HashesPasswordCreatesWalletAndLog()
    {
        var repository = new StubAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            1,
            new AdminUserCreateCommand(
                "New User", "new@example.com", "0901234567", "Password@123",
                Roles.Staff, null, 1),
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
    public async Task CreateAsync_CustomerRole_ReturnsValidationError()
    {
        var repository = new StubAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            1,
            new AdminUserCreateCommand(
                "New Customer", "customer@example.com", "0901234567", "Password@123",
                Roles.Customer, null, null),
            "127.0.0.1");

        Assert.False(result.Succeeded);
        Assert.Equal("Customer accounts cannot be created by admin", result.ErrorMessage);
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task DeleteAsync_UserHasBookings_DeactivatesInsteadOfDeleting()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User { UserID = 2, Role = Roles.Customer, Status = UserStatuses.Active },
            HasDeletionBlockingData = true
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(1, 2, null);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.Deactivated);
        Assert.False(result.Value.PhysicallyDeleted);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(UserStatuses.Inactive, repository.ChangedStatus);
        Assert.Equal(AdminActionTypes.DeleteUser, repository.LastLog!.ActionType);
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
    public async Task DeleteAsync_FinancialDataBlocksDelete_DeactivatesUser()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User { UserID = 2, Role = Roles.Staff, Status = UserStatuses.Active },
            HasDeletionBlockingData = true
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(1, 2, null);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.Deactivated);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(UserStatuses.Inactive, repository.ChangedStatus);
        Assert.Equal(AdminActionTypes.DeleteUser, repository.LastLog!.ActionType);
    }

    [Fact]
    public async Task DeleteAsync_CloudinaryDeleteFails_ReturnsStorageError()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User
            {
                UserID = 2,
                Role = Roles.Customer,
                Status = UserStatuses.Active,
                AvatarPublicId = "avatar-id"
            }
        };
        var service = new AdminUserService(
            repository,
            new StubImageStorageService { DeleteShouldFail = true },
            new StubUnitOfWork(),
            NullLogger<AdminUserService>.Instance);

        var result = await service.DeleteAsync(1, 2, null);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminUserErrorType.Storage, result.ErrorType);
    }

    [Fact]
    public async Task DeleteAsync_EligibleUser_PhysicallyDeletesAndKeepsAuditTargetId()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User { UserID = 2, Role = Roles.Customer, Status = UserStatuses.Active }
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(1, 2, "127.0.0.1");

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.PhysicallyDeleted);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(AdminActionTypes.DeleteUser, repository.LastLog!.ActionType);
        Assert.Null(repository.LastLog.TargetUserID);
        Assert.Equal(2, repository.LastLog.TargetID);
    }

    [Fact]
    public async Task DeleteAsync_AdminDeletesSelf_ReturnsForbidden()
    {
        var repository = new StubAdminUserRepository
        {
            User = new User { UserID = 1, Role = Roles.Admin, Status = UserStatuses.Active }
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(1, 1, null);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminUserErrorType.Forbidden, result.ErrorType);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    private static AdminUserService CreateService(StubAdminUserRepository repository) =>
        new(repository, new StubImageStorageService(), new StubUnitOfWork(),
            NullLogger<AdminUserService>.Instance);

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default) => operation();
    }

    private sealed class StubImageStorageService : IImageStorageService
    {
        public bool DeleteShouldFail { get; init; }

        public Task<StoredImageResult> UploadImageAsync(
            Stream imageStream, string fileName, string folder,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredImageResult("https://example.com/avatar", "avatar-id"));

        public Task DeleteImageAsync(
            string publicId, CancellationToken cancellationToken = default) =>
            DeleteShouldFail
                ? Task.FromException(new InvalidOperationException("Cloudinary failed"))
                : Task.CompletedTask;
    }

    private sealed class StubAdminUserRepository : IAdminUserRepository
    {
        public User? User { get; set; }
        public bool HasDeletionBlockingData { get; set; }
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

        public Task<bool> HasDeletionBlockingDataAsync(
            int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(HasDeletionBlockingData);

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
            actionLog.TargetUserID = null;
            actionLog.TargetID = userId;
            LastLog = actionLog;
            return Task.FromResult(TryDeleteResult);
        }
    }
}
