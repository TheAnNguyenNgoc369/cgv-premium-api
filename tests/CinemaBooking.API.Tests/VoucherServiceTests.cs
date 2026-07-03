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
        var service = new VoucherService(repository, new StubStorage());
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
        var service = new VoucherService(new StubRepository(), new StubStorage());
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
    }

    private sealed class StubStorage : IImageStorageService
    {
        public Task<StoredImageResult> UploadImageAsync(Stream imageStream, string fileName, string folder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
