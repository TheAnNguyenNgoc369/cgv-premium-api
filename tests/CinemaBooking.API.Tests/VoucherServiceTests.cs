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
        var service = new VoucherService(repository, new StubUserVoucherRepository(), new StubUserRepository(), new StubStorage(), new StubVoucherRuleRepository());
        var result = await service.CreateAsync(1,
            new(" summer10 ", "percent", 10, 100_000, 50,
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(7)),
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(7)), null, true, null, null, null),
            null, default);

        Assert.True(result.Succeeded);
        Assert.Equal("SUMMER10", result.Voucher!.VoucherCode);
        Assert.Equal("active", result.Voucher.IsActive ? "active" : "inactive");
    }

    [Fact]
    public async Task Create_PercentAboveOneHundred_ReturnsValidationError()
    {
        var service = new VoucherService(new StubRepository(), new StubUserVoucherRepository(), new StubUserRepository(), new StubStorage(), new StubVoucherRuleRepository());
        var result = await service.CreateAsync(1,
            new("TEST", "percent", 101, null, null,
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(7)),
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(7)), null, true, null, null, null),
            null, default);

        Assert.False(result.Succeeded);
        Assert.Equal("discountValue must be between 1-100 for percent or >= 0 for fixed.", result.Error);
    }

    private sealed class StubRepository : IVoucherRepository
    {
        public Task<(List<Voucher> Items, int Total)> GetPageAsync(string? search, int page, int pageSize, CancellationToken ct) => Task.FromResult((new List<Voucher>(), 0));
        public Task<Voucher?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult<Voucher?>(null);
        public Task<bool> CodeExistsAsync(string code, int? excludingId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> HasTransactionsAsync(int id, CancellationToken ct) => Task.FromResult(false);
        public Task<Voucher> SaveWithRulesAsync(Voucher voucher, bool isNew, IReadOnlyList<VoucherRule> newRules, AdminActionLog log, CancellationToken ct) { voucher.VoucherID = 1; return Task.FromResult(voucher); }
        public Task<bool> DeactivateAsync(int id, AdminActionLog log, CancellationToken ct) => Task.FromResult(true);
        public Task<List<Voucher>> GetRedeemableVouchersAsync(CancellationToken ct) => Task.FromResult(new List<Voucher>());
        public Task<Voucher?> GetForRedemptionAsync(int voucherId, CancellationToken ct) => Task.FromResult<Voucher?>(null);
        public Task<int> GetUserRedemptionCountAsync(int userId, int voucherId, CancellationToken ct) => Task.FromResult(0);
        public Task IncrementPublicVoucherUsageForBookingAsync(int bookingId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubStorage : IImageStorageService
    {
        public Task<StoredImageResult> UploadImageAsync(Stream imageStream, string fileName, string folder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUserVoucherRepository : IUserVoucherRepository
    {
        public Task<List<UserVoucher>> GetUserVouchersAsync(int userId, CancellationToken ct) => Task.FromResult(new List<UserVoucher>());
        public Task<UserVoucher?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<(bool Succeeded, string? Error)> RedeemVoucherAsync(UserVoucher userVoucher, LoyaltyPoints loyaltyPoints, int pointsRedeemed, int? maxUses, int? exchangeLimit, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserVoucher?> GetAvailableOwnedAsync(int userId, int voucherId, DateTime now, CancellationToken ct) => Task.FromResult<UserVoucher?>(null);
        public Task<UserVoucher?> GetAvailableForUpdateAsync(int userId, int voucherId, CancellationToken ct) => Task.FromResult<UserVoucher?>(null);
        public Task MarkReservedAsUsedByBookingAsync(int bookingId, DateTime usedAt, CancellationToken ct) => Task.CompletedTask;
        public Task ReleaseReservedByBookingAsync(int bookingId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubVoucherRuleRepository : IVoucherRuleRepository
    {
        public Task<List<VoucherRule>> GetByVoucherIdAsync(int voucherId, CancellationToken ct) => Task.FromResult(new List<VoucherRule>());
        public Task<VoucherRule?> GetByIdAsync(int ruleId, CancellationToken ct) => Task.FromResult<VoucherRule?>(null);
        public Task<VoucherRule> AddAsync(VoucherRule rule, CancellationToken ct) => Task.FromResult(rule);
        public Task<bool> DeleteAsync(int ruleId, CancellationToken ct) => Task.FromResult(true);
        public Task<List<VoucherRule>> GetByRuleTypeAsync(int voucherId, string ruleType, CancellationToken ct) => Task.FromResult(new List<VoucherRule>());
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult<User?>(null);
        public Task<User?> GetProfileByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> LookupCustomerAsync(string? email, string? phone, string? barcode, CancellationToken ct) => throw new NotSupportedException();
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
        public Task<bool> BarCodeExistsAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
