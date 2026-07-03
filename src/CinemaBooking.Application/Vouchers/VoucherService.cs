using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers;

public sealed class VoucherService : IVoucherService
{
    private const string ImageFolder = "cgvp/vouchers";
    private static readonly string[] Categories = ["Discount", "Combo", "Cashback"];
    private readonly IVoucherRepository _repository;
    private readonly IImageStorageService _imageStorage;

    public VoucherService(IVoucherRepository repository, IImageStorageService imageStorage)
    { _repository = repository; _imageStorage = imageStorage; }

    public async Task<VoucherPage> GetAsync(string? search, int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        pageIndex = Math.Max(1, pageIndex); pageSize = Math.Clamp(pageSize, 1, 100);
        var data = await _repository.GetPageAsync(search?.Trim(), pageIndex, pageSize, cancellationToken);
        return new(data.Items, pageIndex, pageSize, data.Total);
    }

    public Task<VoucherResult> CreateAsync(int adminId, VoucherCommand command, Stream? image, string? fileName,
        string? contentType, long fileSize, string? ip, CancellationToken cancellationToken) =>
        SaveAsync(adminId, null, command, image, fileName, contentType, fileSize, ip, cancellationToken);

    public Task<VoucherResult> UpdateAsync(int adminId, int id, VoucherCommand command, Stream? image, string? fileName,
        string? contentType, long fileSize, string? ip, CancellationToken cancellationToken) =>
        SaveAsync(adminId, id, command, image, fileName, contentType, fileSize, ip, cancellationToken);

    private async Task<VoucherResult> SaveAsync(int adminId, int? id, VoucherCommand c, Stream? image,
        string? fileName, string? contentType, long fileSize, string? ip, CancellationToken ct)
    {
        var code = c.VoucherCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 50) return Fail("VoucherCode is required and must not exceed 50 characters");
        var type = c.DiscountType?.Trim().ToLowerInvariant();
        if (type is not ("percent" or "fixed")) return Fail("DiscountType must be percent or fixed");
        if (c.DiscountValue < 0 || type == "percent" && c.DiscountValue > 100) return Fail("DiscountValue is invalid");
        if (c.Category is not null && !Categories.Contains(c.Category)) return Fail("Category is invalid");
        if (c.MinOrderValue < 0 || c.MaxUses <= 0) return Fail("MinOrderValue and MaxUses are invalid");
        if (c.ValidFrom.Offset != TimeSpan.FromHours(7) || c.ValidUntil.Offset != TimeSpan.FromHours(7)) return Fail("Voucher dates must use +07:00 offset");
        if (c.ValidFrom >= c.ValidUntil) return Fail("ValidFrom must be before ValidUntil");
        Voucher? existing = null;
        if (id.HasValue) { existing = await _repository.GetByIdAsync(id.Value, ct); if (existing is null) return new(false, "Voucher not found", null, "not_found"); }
        if (existing is not null && !string.Equals(existing.VoucherCode, code, StringComparison.OrdinalIgnoreCase)
            && await _repository.HasTransactionsAsync(existing.VoucherID, ct)) return new(false, "VoucherCode cannot be changed after transactions exist", null, "conflict");
        if (await _repository.CodeExistsAsync(code, id, ct)) return new(false, "VoucherCode already exists", null, "conflict");
        if (existing is not null && c.MaxUses.HasValue && c.MaxUses < existing.UsedCount) return Fail("MaxUses cannot be less than UsedCount");

        StoredImageResult? upload = null;
        if (image is not null)
        {
            var error = ImageFileValidator.Validate(fileName!, contentType, fileSize); if (error is not null) return Fail(error);
            upload = await _imageStorage.UploadImageAsync(image, fileName!, ImageFolder, ct);
        }
        var voucher = existing ?? new Voucher { UsedCount = 0, CreatedAt = DateTime.UtcNow };
        var oldPublicId = existing?.ImagePublicId;
        voucher.VoucherCode = code; voucher.Category = c.Category; voucher.DiscountType = type;
        voucher.DiscountValue = c.DiscountValue; voucher.MinOrderValue = c.MinOrderValue; voucher.MaxUses = c.MaxUses;
        voucher.ValidFrom = c.ValidFrom.UtcDateTime; voucher.ValidUntil = c.ValidUntil.UtcDateTime;
        voucher.Description = c.Description?.Trim(); voucher.IsActive = c.IsActive;
        if (upload is not null) { voucher.ImageURL = upload.SecureUrl; voucher.ImagePublicId = upload.PublicId; }
        var log = Log(adminId, id is null ? AdminActionTypes.CreateVoucher : AdminActionTypes.UpdateVoucher, id, ip);
        try { voucher = id is null ? await _repository.AddAsync(voucher, log, ct) : (await _repository.UpdateAsync(voucher, log, ct))!; }
        catch { if (upload is not null) await _imageStorage.DeleteImageAsync(upload.PublicId, CancellationToken.None); throw; }
        if (upload is not null && !string.IsNullOrWhiteSpace(oldPublicId))
        {
            try { await _imageStorage.DeleteImageAsync(oldPublicId, ct); }
            catch (Exception exception) when (exception is not OperationCanceledException) { }
        }
        return new(true, null, voucher);
    }

    public async Task<VoucherResult> DeleteAsync(int adminId, int id, string? ip, CancellationToken ct) =>
        await _repository.DeactivateAsync(id, Log(adminId, AdminActionTypes.DeleteVoucher, id, ip), ct)
            ? new(true, null, null) : new(false, "Voucher not found", null, "not_found");

    private static AdminActionLog Log(int adminId, string action, int? id, string? ip) => new()
    { AdminID = adminId, TargetTable = "Voucher", TargetID = id, ActionType = action, Description = $"{action} voucher", IPAddress = ip, CreatedAt = DateTime.UtcNow };
    private static VoucherResult Fail(string error) => new(false, error, null);
}
