using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Vouchers;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class VoucherServiceTests
{
    [Fact]
    public async Task Create_PercentWithinRange_CreatesActiveVoucher()
    {
        var repository = new StubRepository();
        var service = new VoucherService(repository, new StubUserVoucherRepository(), new StubUserRepository(), new StubStorage());
        var result = await service.CreateAsync(1,
            new(" summer10 ", "Discount", "percent", 10, 100_000, 50,
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(7)),
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(7)), null, true),
            null, null, null, 0, null, default);

        Assert.True(result.Succeeded);
        Assert.Equal("SUMMER10", result.Voucher!.VoucherCode);
        Assert.Equal("active", result.Voucher.IsActive ? "active" : "inactive");
    }

    [Fact]
    public async Task Create_PercentAboveOneHundred_ReturnsValidationError()
    {
        var service = new VoucherService(new StubRepository(), new StubUserVoucherRepository(), new StubUserRepository(), new StubStorage());
        var result = await service.CreateAsync(1,
            new("TEST", "Discount", "percent", 101, null, null,
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(7)),
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(7)), null, true),
            null, null, null, 0, null, default);

        Assert.False(result.Succeeded);
        Assert.Equal("DiscountValue is invalid", result.Error);
    }

    private sealed class StubRepository : IVoucherRepository
    {
        public Task<(List<Voucher> Items, int Total)> GetPageAsync(string? search, int page, int pageSize, CancellationToken ct) => Task.FromResult((new List<Voucher>(), 0));
        public Task<Voucher?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult<Voucher?>(null);
        public Task<bool> CodeExistsAsync(string code, int? excludingId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> HasTransactionsAsync(int id, CancellationToken ct) => Task.FromResult(false);
        public Task<Voucher> AddAsync(Voucher voucher, AdminActionLog log, CancellationToken ct) { voucher.VoucherID = 1; return Task.FromResult(voucher); }
        public Task<Voucher?> UpdateAsync(Voucher voucher, AdminActionLog log, CancellationToken ct) => Task.FromResult<Voucher?>(voucher);
        public Task<bool> DeactivateAsync(int id, AdminActionLog log, CancellationToken ct) => Task.FromResult(true);
        public Task<List<Voucher>> GetRedeemableVouchersAsync(CancellationToken ct) => Task.FromResult(new List<Voucher>());
        public Task<Voucher?> GetForRedemptionAsync(int voucherId, CancellationToken ct) => Task.FromResult<Voucher?>(null);
        public Task<int> GetUserRedemptionCountAsync(int userId, int voucherId, CancellationToken ct) => Task.FromResult(0);
    }

    private sealed class StubStorage : IImageStorageService
    {
        public Task<StoredImageResult> UploadImageAsync(Stream imageStream, string fileName, string folder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUserVoucherRepository : IUserVoucherRepository
    {
        public Task<List<UserVoucher>> GetUserVouchersAsync(int userId, CancellationToken ct) => Task.FromResult(new List<UserVoucher>());
        public Task<UserVoucher?> GetUserVoucherAsync(int userId, int voucherId, CancellationToken ct) => Task.FromResult<UserVoucher?>(null);
        public Task AddUserVoucherAsync(UserVoucher userVoucher, CancellationToken ct) => Task.CompletedTask;
        public Task<UserVoucher?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task RedeemVoucherAsync(UserVoucher userVoucher, LoyaltyPoints loyaltyPoints, int pointsRedeemed, AdminActionLog actionLog, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult<User?>(null);
        public Task<User?> GetProfileByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> LookupCustomerAsync(string? email, string? phone, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> PhoneExistsAsync(string phone, int? excludingUserId, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> UpdateProfileAsync(int userId, string fullName, string? phoneNumber, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> UpdateAvatarAsync(int userId, string? avatarUrl, string? publicId, CancellationToken ct) => throw new NotSupportedException();
        public Task<Wallet?> GetWalletByUserIdAsync(int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task AddUserWithWalletAsync(User user, Wallet wallet, CancellationToken ct) => throw new NotSupportedException();
        public Task AddUserWithWalletAndVerificationTokenAsync(User user, Wallet wallet, EmailVerificationToken token, CancellationToken ct) => throw new NotSupportedException();
        public Task AddEmailVerificationTokenAsync(EmailVerificationToken token, CancellationToken ct) => throw new NotSupportedException();
        public Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string token, CancellationToken ct) => throw new NotSupportedException();
        public Task<EmailVerificationToken?> GetLatestEmailVerificationTokenAsync(int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken ct) => throw new NotSupportedException();
        public Task<PasswordResetToken?> GetLatestPasswordResetTokenAsync(int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task ReplaceUnusedPasswordResetTokensAsync(int userId, PasswordResetToken newToken, CancellationToken ct) => throw new NotSupportedException();
        public Task ReplaceUnverifiedEmailVerificationTokensAsync(int userId, EmailVerificationToken newToken, CancellationToken ct) => throw new NotSupportedException();
        public Task DeleteEmailVerificationTokenAsync(string token, CancellationToken ct) => throw new NotSupportedException();
        public Task DeletePasswordResetTokenAsync(string token, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryResetPasswordAsync(string token, string newPasswordHash, DateTime now, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryIncrementTokenVersionAsync(int userId, int expectedVersion, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryUpdatePasswordHashAsync(int userId, string oldPasswordHash, string newPasswordHash, CancellationToken ct) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken ct) => throw new NotSupportedException();
    }
}
