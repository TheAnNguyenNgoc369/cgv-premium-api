namespace CinemaBooking.Application.Common.Security;

public interface IManagerCinemaScopeService
{
    Task<int?> GetAssignedCinemaIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<int?> GetAssignedCinemaIdAsync(
        int userId,
        string role,
        CancellationToken cancellationToken = default);
}
