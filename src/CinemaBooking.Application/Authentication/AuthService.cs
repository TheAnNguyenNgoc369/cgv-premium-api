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
    private const int VerificationEmailResendCooldownSeconds = 60;

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

        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = normalizedFullName,
            Email = normalizedEmail,
            Phone = normalizedPhone,
            PasswordHash = HashPassword(password),
            Role = Roles.Customer,
            Status = ActiveStatus,
            EmailVerifiedAt = null,
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

        if (!user.EmailVerifiedAt.HasValue)
        {
            return (false, "Please verify your email before logging in.", null);
        }

        return (true, null, user);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, bool VerificationEmailSent)> ResendVerificationEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Vui lòng nhập email", false);
        }

        var normalizedEmail = email.Trim();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return (false, "Email chưa được đăng ký", false);
        }

        var now = DateTime.UtcNow;

        if (user.EmailVerifiedAt.HasValue)
        {
            return (false, "Email is already verified.", false);
        }

        var lastToken = await _userRepository.GetLatestEmailVerificationTokenAsync(
            user.UserID,
            cancellationToken);

        if (lastToken is not null
            && lastToken.CreatedAt > now.AddSeconds(-VerificationEmailResendCooldownSeconds))
        {
            return (false, "Please wait before requesting another verification email.", false);
        }

        var verificationToken = new EmailVerificationToken
        {
            Token = GenerateEmailVerificationToken(),
            ExpiresAt = now.AddHours(VerificationTokenExpirationHours),
            CreatedAt = now,
            UserID = user.UserID
        };

        await _userRepository.ReplaceUnverifiedEmailVerificationTokensAsync(
            user.UserID,
            verificationToken,
            cancellationToken);

        var verificationEmailSent = await _emailSender.SendAsync(
            user.Email,
            "Verify your Cinema Booking account",
            BuildVerificationEmailBody(user.FullName, verificationToken.Token),
            cancellationToken);

        return (true, null, verificationEmailSent);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Vui lòng nhập email");
        }

        var normalizedEmail = email.Trim();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return (true, null);
        }

        var now = DateTime.UtcNow;
        var resetToken = new PasswordResetToken
        {
            Token = GeneratePasswordResetToken(),
            ExpiresAt = now.AddMinutes(15),
            CreatedAt = now,
            UserID = user.UserID
        };

        await _userRepository.AddPasswordResetTokenAsync(resetToken, cancellationToken);

        var emailSent = await _emailSender.SendAsync(
            user.Email,
            "Reset your Cinema Booking password",
            BuildResetPasswordEmailBody(user.FullName, resetToken.Token),
            cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> ResetPasswordAsync(
        string token,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Reset token is required");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return (false, "Vui lòng nhập mật khẩu mới");
        }

        if (newPassword.Length < 6)
        {
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return (false, "Mật khẩu xác nhận không khớp");
        }

        var resetToken = await _userRepository.GetPasswordResetTokenAsync(
            token.Trim(),
            cancellationToken);

        if (resetToken is null)
        {
            return (false, "Reset token is invalid");
        }

        if (resetToken.UsedAt.HasValue)
        {
            return (false, "Reset token has already been used");
        }

        var now = DateTime.UtcNow;

        if (resetToken.ExpiresAt < now)
        {
            return (false, "Reset token has expired");
        }

        resetToken.User.PasswordHash = HashPassword(newPassword);
        resetToken.User.UpdatedAt = now;
        resetToken.UsedAt = now;

        await _userRepository.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Token is required");
        }

        var verificationToken = await _userRepository.GetEmailVerificationTokenAsync(
            token.Trim(),
            cancellationToken);

        if (verificationToken is null)
        {
            return (false, "Token không hợp lệ");
        }

        if (verificationToken.VerifiedAt.HasValue)
        {
            return (false, "Email đã được xác thực trước đó");
        }

        var now = DateTime.UtcNow;

        if (verificationToken.ExpiresAt <= now)
        {
            return (false, "Token đã hết hạn");
        }

        if (verificationToken.User is null)
        {
            return (false, "Token is invalid");
        }

        if (verificationToken.User.EmailVerifiedAt.HasValue)
        {
            return (false, "Email is already verified.");
        }

        verificationToken.User.EmailVerifiedAt = now;
        verificationToken.User.UpdatedAt = now;
        verificationToken.VerifiedAt = now;
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

    private static string GeneratePasswordResetToken()
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
        var verifyUrl = $"http://localhost:5173/register?token={Uri.EscapeDataString(token)}";

        return $"""
            <div style="font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #e0e0e0;">

                <div style="background: #c62828; padding: 22px 32px; text-align: center;">
                    <span style="color: #ffffff; font-size: 20px; font-weight: 700; letter-spacing: 1px;">CGV Premium</span>
                </div>

                <div style="padding: 32px 36px 24px;">
                    <p style="margin: 0 0 6px; color: #111111; font-size: 20px; font-weight: 700;">Verify your email</p>
                    <p style="margin: 0 0 24px; color: #555555; font-size: 14px; line-height: 1.6;">
                        Hello <strong>{encodedFullName}</strong>, welcome to CGV Premium!<br />
                        Click the button below to activate your account.
                    </p>

                    <div style="text-align: center; margin: 0 0 24px;">
                        <a href="{verifyUrl}" style="display: inline-block; background: #c62828; color: #ffffff; font-size: 15px; font-weight: 700; text-decoration: none; padding: 14px 40px; border-radius: 6px;">
                            Verify my account
                        </a>
                        <p style="margin: 12px 0 0; font-size: 12px; color: #999999;">This link expires in {VerificationTokenExpirationHours} hours</p>
                    </div>

                    <div style="background: #fff8e1; border-left: 3px solid #f9a825; border-radius: 0 4px 4px 0; padding: 10px 14px;">
                        <p style="margin: 0; font-size: 13px; color: #7a6000; line-height: 1.5;">
                            If you did not create this account, you can safely ignore this email.
                        </p>
                    </div>
                </div>

                <div style="background: #f9f9f9; border-top: 1px solid #eeeeee; padding: 14px 36px; text-align: center;">
                    <p style="margin: 0 0 2px; font-size: 11px; color: #aaaaaa;">&copy; {DateTime.UtcNow.Year} CGV Premium. All rights reserved.</p>
                    <p style="margin: 0; font-size: 11px; color: #bbbbbb;">This is an automated message &mdash; please do not reply to this email.</p>
                </div>

            </div>
            """;
    }

    private static string BuildResetPasswordEmailBody(string fullName, string token)
    {
        var encodedFullName = WebUtility.HtmlEncode(fullName);
        var resetUrl = $"http://localhost:5173/resetPassword?token={Uri.EscapeDataString(token)}";

        return $"""
            <div style="font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #e0e0e0;">

                <div style="background: #c62828; padding: 22px 32px; text-align: center;">
                    <span style="color: #ffffff; font-size: 20px; font-weight: 700; letter-spacing: 1px;">CGV Premium</span>
                </div>

                <div style="padding: 32px 36px 24px;">
                    <p style="margin: 0 0 6px; color: #111111; font-size: 20px; font-weight: 700;">Reset your password</p>
                    <p style="margin: 0 0 24px; color: #555555; font-size: 14px; line-height: 1.6;">
                        Hello <strong>{encodedFullName}</strong>,<br />
                        Click the button below to reset your password.
                    </p>

                    <div style="text-align: center; margin: 0 0 24px;">
                        <a href="{resetUrl}" style="display: inline-block; background: #c62828; color: #ffffff; font-size: 15px; font-weight: 700; text-decoration: none; padding: 14px 40px; border-radius: 6px;">
                            Reset my password
                        </a>
                        <p style="margin: 12px 0 0; font-size: 12px; color: #999999;">This link expires in 15 minutes</p>
                    </div>

                    <div style="background: #fff8e1; border-left: 3px solid #f9a825; border-radius: 0 4px 4px 0; padding: 10px 14px;">
                        <p style="margin: 0; font-size: 13px; color: #7a6000; line-height: 1.5;">
                            If you did not request a password reset, you can ignore this email. The link will automatically expire.
                        </p>
                    </div>
                </div>

                <div style="background: #f9f9f9; border-top: 1px solid #eeeeee; padding: 14px 36px; text-align: center;">
                    <p style="margin: 0 0 2px; font-size: 11px; color: #aaaaaa;">&copy; {DateTime.UtcNow.Year} CGV Premium. All rights reserved.</p>
                </div>

            </div>
            """;
    }
}
