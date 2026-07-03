namespace CinemaBooking.Application.Vouchers;

public interface IVoucherService
{
    Task<VoucherPage> GetAsync(string? search, int pageIndex, int pageSize, CancellationToken cancellationToken);
    Task<VoucherResult> CreateAsync(int adminId, VoucherCommand command, Stream? image, string? fileName, string? contentType, long fileSize, string? ip, CancellationToken cancellationToken);
    Task<VoucherResult> UpdateAsync(int adminId, int id, VoucherCommand command, Stream? image, string? fileName, string? contentType, long fileSize, string? ip, CancellationToken cancellationToken);
    Task<VoucherResult> DeleteAsync(int adminId, int id, string? ip, CancellationToken cancellationToken);
}
