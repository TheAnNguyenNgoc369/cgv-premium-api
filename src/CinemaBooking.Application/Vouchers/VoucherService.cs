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
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly IUserRepository _userRepository;
    private readonly IImageStorageService _imageStorage;

    public VoucherService(IVoucherRepository repository, IUserVoucherRepository userVoucherRepository,
        IUserRepository userRepository, IImageStorageService imageStorage)
    { _repository = repository; _userVoucherRepository = userVoucherRepository; _userRepository = userRepository; _imageStorage = imageStorage; }

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
        if (string.IsNullOrWhiteSpace(code)) return Fail("voucherCode is required and cannot be null.");
        if (code.Length > 50) return Fail("voucherCode is invalid. Maximum length is 50 characters.");
        var type = c.DiscountType?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(type)) return Fail("discountType is required and cannot be null.");
        if (type is not ("percent" or "fixed")) return Fail("discountType is invalid. Allowed values: percent, fixed.");
        if (c.DiscountValue < 0 || type == "percent" && c.DiscountValue > 100) return Fail("discountValue must be between 0-100 for percent or >= 0 for fixed.");
        if (c.Category is not null && !Categories.Contains(c.Category)) return Fail("category is invalid. Allowed values: Discount, Combo, Cashback.");
        if (c.MinOrderValue < 0) return Fail("MinOrderValue must be greater than or equal to 0.");
        if (c.MaxUses <= 0) return Fail("maxUses must be greater than 0.");
        if (c.ValidFrom.Offset != TimeSpan.FromHours(7)) return Fail("validFrom is invalid. Use ISO 8601 format with +07:00.");
        if (c.ValidUntil.Offset != TimeSpan.FromHours(7)) return Fail("validUntil is invalid. Use ISO 8601 format with +07:00.");
        if (c.ValidFrom >= c.ValidUntil) return Fail("validUntil must be a date after validFrom (ISO 8601 format with +07:00).");
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

    public async Task<RedeemableVouchersResult> GetRedeemableVouchersAsync(CancellationToken ct)
    {
        try
        {
            var vouchers = await _repository.GetRedeemableVouchersAsync(ct);
            return new(true, vouchers);
        }
        catch (Exception ex)
        {
            return new(false, [], $"Failed to retrieve redeemable vouchers: {ex.Message}");
        }
    }

    public async Task<RedeemVoucherResult> RedeemVoucherAsync(int userId, int voucherId, string? ip, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null) return new(false, 0, string.Empty, "User not found", "not_found");
        if (user.Role != Roles.Customer) return new(false, 0, string.Empty, "Only customers can redeem vouchers", "forbidden");
        if (user.Status != UserStatuses.Active) return new(false, 0, string.Empty, "User account is not active", "forbidden");

        var voucher = await _repository.GetForRedemptionAsync(voucherId, ct);
        if (voucher is null) return new(false, 0, string.Empty, "Voucher not found", "not_found");
        if (!voucher.IsActive) return new(false, 0, string.Empty, "Voucher is not active", "validation");
        if (!voucher.IsRedeemable) return new(false, 0, string.Empty, "Voucher is not redeemable", "validation");
        if (!voucher.RequiredPoints.HasValue) return new(false, 0, string.Empty, "Voucher cannot be redeemed with points", "validation");

        var now = DateTime.UtcNow;
        if (now < voucher.ValidFrom) return new(false, 0, string.Empty, "Voucher is not yet valid", "validation");
        if (now > voucher.ValidUntil) return new(false, 0, string.Empty, "Voucher has expired", "validation");

        if (voucher.RemainingQuantity.HasValue && voucher.RemainingQuantity <= 0)
            return new(false, 0, string.Empty, "Voucher is out of stock", "validation");

        if (user.TotalPoints < voucher.RequiredPoints.Value)
            return new(false, user.TotalPoints, string.Empty, $"Insufficient points. Required: {voucher.RequiredPoints.Value}, Available: {user.TotalPoints}", "validation");

        if (voucher.ExchangeLimit.HasValue)
        {
            var redemptionCount = await _repository.GetUserRedemptionCountAsync(userId, voucherId, ct);
            if (redemptionCount >= voucher.ExchangeLimit.Value)
                return new(false, user.TotalPoints, string.Empty, $"Exchange limit reached. Maximum {voucher.ExchangeLimit.Value} redemptions per user", "validation");
        }

        var userVoucher = new UserVoucher
        {
            UserID = userId,
            VoucherID = voucherId,
            RedeemedAt = now,
            ExpiredAt = voucher.ValidUntil,
            Status = UserVoucherStatus.Available
        };

        var loyaltyPoint = new LoyaltyPoints
        {
            UserID = userId,
            VoucherID = voucherId,
            PointsDelta = -voucher.RequiredPoints.Value,
            TransactionType = LoyaltyTransactionTypes.Exchange,
            Description = $"Redeemed voucher: {voucher.VoucherCode}",
            CreatedAt = now
        };

        var log = new AdminActionLog
        {
            AdminID = userId,
            TargetTable = "UserVoucher",
            ActionType = AdminActionTypes.RedeemVoucher,
            Description = $"User redeemed voucher {voucher.VoucherCode}",
            IPAddress = ip,
            CreatedAt = now
        };

        try
        {
            await _userVoucherRepository.RedeemVoucherAsync(userVoucher, loyaltyPoint, voucher.RequiredPoints.Value, log, ct);
            return new(true, user.TotalPoints - voucher.RequiredPoints.Value, voucher.VoucherCode);
        }
        catch (Exception ex)
        {
            return new(false, user.TotalPoints, string.Empty, $"Failed to redeem voucher: {ex.Message}", "error");
        }
    }

    public async Task<UserVouchersResult> GetUserVouchersAsync(int userId, CancellationToken ct)
    {
        try
        {
            var vouchers = await _userVoucherRepository.GetUserVouchersAsync(userId, ct);
            return new(true, vouchers);
        }
        catch (Exception ex)
        {
            return new(false, [], $"Failed to retrieve user vouchers: {ex.Message}");
        }
    }

    private static AdminActionLog Log(int adminId, string action, int? id, string? ip) => new()
    { AdminID = adminId, TargetTable = "Voucher", TargetID = id, ActionType = action, Description = $"{action} voucher", IPAddress = ip, CreatedAt = DateTime.UtcNow };
    private static VoucherResult Fail(string error) => new(false, error, null);
}
