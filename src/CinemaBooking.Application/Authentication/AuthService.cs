using System.Security.Cryptography;
using System.Text;
using System.Net;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Authentication;

public sealed class AuthService : IAuthService
{
    private const string ActiveStatus = "active";
    private const string LockedStatus = "locked";
    private const string InactiveStatus = "inactive";
    private const int VerificationTokenExpirationHours = 24;

    private readonly IUserRepository _userRepository;
    private readonly IEmailSender _emailSender;

    public AuthService(
        IUserRepository userRepository,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _emailSender = emailSender;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, int? UserId, bool VerificationEmailSent)> RegisterAsync(
        string fullName,
        string email,
        string phone,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedFullName = fullName.Trim();
        var normalizedEmail = email.Trim();
        var normalizedPhone = phone.Trim();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return (false, "Email này đã được đăng ký", null, false);
        }

        if (await _userRepository.PhoneExistsAsync(normalizedPhone, cancellationToken))
        {
            return (false, "Số điện thoại này đã được đăng ký", null, false);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = normalizedFullName,
            Email = normalizedEmail,
            Phone = normalizedPhone,
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

        var verificationToken = new EmailVerificationToken
        {
            Token = GenerateEmailVerificationToken(),
            ExpiresAt = now.AddHours(VerificationTokenExpirationHours),
            CreatedAt = now
        };

        await _userRepository.AddUserWithWalletAndVerificationTokenAsync(
            user,
            wallet,
            verificationToken,
            cancellationToken);

        var verificationEmailSent = await _emailSender.SendAsync(
            user.Email,
            "Verify your Cinema Booking account",
            BuildVerificationEmailBody(user.FullName, verificationToken.Token),
            cancellationToken);

        return (true, null, user.UserID, verificationEmailSent);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, User? User)> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email.Trim(), cancellationToken);

        if (user is null || user.PasswordHash != HashPassword(password))
        {
            return (false, "Email hoặc mật khẩu không đúng", null);
        }

        if (string.Equals(user.Status, LockedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Tài khoản đã bị khoá. Vui lòng liên hệ hỗ trợ", null);
        }

        if (string.Equals(user.Status, InactiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Tài khoản chưa được kích hoạt", null);
        }

        return (true, null, user);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Verification token is required");
        }

        var verificationToken = await _userRepository.GetEmailVerificationTokenAsync(
            token.Trim(),
            cancellationToken);

        if (verificationToken is null)
        {
            return (false, "Verification token is invalid");
        }

        if (verificationToken.VerifiedAt.HasValue)
        {
            return (true, null);
        }

        var now = DateTime.UtcNow;

        if (verificationToken.ExpiresAt < now)
        {
            return (false, "Verification token has expired");
        }

        verificationToken.VerifiedAt = now;

        if (!string.Equals(verificationToken.User.Status, LockedStatus, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(verificationToken.User.Status, InactiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            verificationToken.User.Status = ActiveStatus;
            verificationToken.User.UpdatedAt = now;
        }

        await _userRepository.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateEmailVerificationToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string BuildVerificationEmailBody(string fullName, string token)
    {
        var encodedFullName = WebUtility.HtmlEncode(fullName);

        return $"""
            <p>Hello {encodedFullName},</p>
            <p>Please verify your Cinema Booking account using this token:</p>
            <p><strong>{token}</strong></p>
            <p>This token expires in {VerificationTokenExpirationHours} hours.</p>
            """;
    }
}
