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
        var verificationToken = new EmailVerificationToken
        {
            Token = GenerateEmailVerificationToken(),
            ExpiresAt = now.AddHours(VerificationTokenExpirationHours),
            CreatedAt = now,
            UserID = user.UserID
        };

        await _userRepository.AddEmailVerificationTokenAsync(verificationToken, cancellationToken);

        var verificationEmailSent = await _emailSender.SendAsync(
            user.Email,
            "Verify your Cinema Booking account",
            BuildVerificationEmailBody(user.FullName, verificationToken.Token),
            cancellationToken);

        return (true, null, verificationEmailSent);
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
    <div style="font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #e0e0e0;">

        <!-- Header -->
        <div style="background: #c62828; padding: 28px 32px; text-align: center;">
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin: 0 auto;">
                <tr>
                    <td style="vertical-align: middle; padding-right: 10px;">
                        <img src="https://your-cdn.com/cgv-icon.png" alt="" width="28" height="28"
                             style="display: block;"
                             onerror="this.style.display='none'" />
                    </td>
                    <td style="vertical-align: middle;">
                        <span style="color: #ffffff; font-size: 20px; font-weight: 700; letter-spacing: 1px;">CGV Premium</span>
                    </td>
                </tr>
            </table>
        </div>

        <!-- Body -->
        <div style="padding: 36px 40px 28px;">
            <p style="margin: 0 0 8px; color: #111111; font-size: 22px; font-weight: 700;">
                Verify your email
            </p>
            <p style="margin: 0 0 24px; color: #555555; font-size: 15px; line-height: 1.6;">
                Hello <strong>{encodedFullName}</strong>, welcome to CGV Premium!<br />
                Use the code below to activate your account.
            </p>

            <!-- OTP Box -->
            <div style="
                background: #fafafa;
                border: 1px solid #e8e8e8;
                border-top: 3px solid #c62828;
                border-radius: 6px;
                padding: 28px 20px;
                text-align: center;
                margin: 0 0 28px;">
                <p style="margin: 0 0 12px; font-size: 12px; font-weight: 600; letter-spacing: 1.5px; color: #888888; text-transform: uppercase;">
                    Verification code
                </p>
                <p style="
                    margin: 0;
                    font-size: 32px;
                    font-weight: 700;
                    letter-spacing: 10px;
                    color: #c62828;">
                    {token}
                </p>
                <p style="margin: 16px 0 0; font-size: 13px; color: #999999;">
                    Expires in <strong style="color: #555555;">{VerificationTokenExpirationHours} hours</strong>
                </p>
            </div>

            <!-- Warning Notice -->
            <div style="
                background: #fff8e1;
                border-left: 3px solid #f9a825;
                border-radius: 4px;
                padding: 12px 16px;
                margin-bottom: 24px;">
                <p style="margin: 0; font-size: 13px; color: #7a6000; line-height: 1.5;">
                    If you did not create this account, you can safely ignore this email.
                    Your account will not be activated without verification.
                </p>
            </div>

            <p style="margin: 0; color: #555555; font-size: 14px; line-height: 1.7;">
                Thank you for joining CGV Premium. We look forward to bringing you the best cinema experience.
            </p>
        </div>

        <!-- Footer -->
        <div style="
            background: #f9f9f9;
            border-top: 1px solid #eeeeee;
            padding: 20px 40px;
            text-align: center;">
            <p style="margin: 0 0 4px; font-size: 12px; color: #aaaaaa;">
                &copy; {DateTime.UtcNow.Year} CGV Premium. All rights reserved.
            </p>
            <p style="margin: 0; font-size: 12px; color: #bbbbbb;">
                This is an automated message &mdash; please do not reply directly to this email.
            </p>
        </div>

    </div>
    """;

    }
}
