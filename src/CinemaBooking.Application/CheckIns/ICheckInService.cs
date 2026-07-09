namespace CinemaBooking.Application.CheckIns;

public interface ICheckInService
{
    Task<(bool Succeeded, string? ErrorMessage, CheckInLookupResult? Data)> LookupAsync(
        string qrCode,
        int staffId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, string? BookingCode, DateTime? CheckedInAt)> CheckInAsync(
        string qrCode,
        int staffId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<CheckInHistoryResult> GetHistoryAsync(
        int? staffId,
        int? cinemaId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        int currentUserId,
        bool isAdmin,
        bool isManager,
        bool isStaff,
        CancellationToken cancellationToken = default);
}
