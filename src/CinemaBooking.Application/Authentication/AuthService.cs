using System.Security.Cryptography;
using System.Text;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Authentication;

public sealed class AuthService : IAuthService
{
    private const string ActiveStatus = "active";
    private const string LockedStatus = "locked";
    private const string InactiveStatus = "inactive";

    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, int? UserId)> RegisterAsync(
        string fullName,
        string email,
        string phone,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return (false, "Email này đã được đăng ký", null);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = HashPassword(password),
            Role = Roles.Customer,
            Status = ActiveStatus,
            TotalPoints = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        var wallet = new Wallet
        {
            Balance = 0m
        };

        await _userRepository.AddUserWithWalletAsync(user, wallet, cancellationToken);

        return (true, null, user.UserID);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, User? User)> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || user.PasswordHash != HashPassword(password))
        {
            return (false, "Email hoặc mật khẩu không đúng", null);
        }

        if (user.Status == LockedStatus)
        {
            return (false, "Tài khoản đã bị khoá. Vui lòng liên hệ hỗ trợ", null);
        }

        if (user.Status == InactiveStatus)
        {
            return (false, "Tài khoản chưa được kích hoạt", null);
        }

        return (true, null, user);
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
