using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Users;

public interface IUserService
{
    Task<User?> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, User? User)> UpdateProfileAsync(
        int userId,
        string fullName,
        string? phone,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, User? User)> UploadAvatarAsync(
        int userId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, User? User)> DeleteAvatarAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
