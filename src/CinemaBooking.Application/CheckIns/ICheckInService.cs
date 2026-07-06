namespace CinemaBooking.Application.CheckIns;

public interface ICheckInService
{
    Task<(bool Succeeded, string? ErrorMessage, CheckInLookupResult? Data)> LookupAsync(
        string qrCode,
        int staffId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, string? BookingCode, DateTime? CheckedInAt)> CheckInAsync(
        int bookingId,
        int staffId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
