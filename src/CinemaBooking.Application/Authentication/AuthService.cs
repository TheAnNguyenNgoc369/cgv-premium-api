using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Application.Configuration;
using CinemaBooking.Application.Notifications;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.Extensions.Options;

namespace CinemaBooking.Application.Authentication;

public sealed class AuthService : IAuthService
{
    private const string ActiveStatus = "active";
    private const string LockedStatus = "locked";
    private const string InactiveStatus = "inactive";
    private const int VerificationTokenExpirationMinutes = 10;
    private const int VerificationEmailResendCooldownSeconds = 60;
    private const int PasswordResetTokenExpirationMinutes = 15;
    private const int ForgotPasswordCooldownSeconds = 60;
    private const string VerificationEmailSentMessage = "Verification email has been sent.";
    private const string PasswordResetEmailSentMessage = "Password reset email has been sent.";
    private const string EmailNotFoundMessage = "Email doesn't exist.";
    private const string EmailAlreadyVerifiedMessage = "Email has been verified.";
    private const string EmailSendFailedMessage = "Email could not be sent. Please try again later.";

    private readonly IUserRepository _userRepository;
    private readonly IEmailSender _emailSender;
    private readonly IAuthEmailService _authEmailService;
    private readonly FrontendSettings _frontendSettings;

    public AuthService(
        IUserRepository userRepository,
        IEmailSender emailSender,
        IAuthEmailService authEmailService,
        IOptions<FrontendSettings>? frontendSettings = null)
    {
        _userRepository = userRepository;
        _emailSender = emailSender;
        _authEmailService = authEmailService;
        _frontendSettings = frontendSettings?.Value ?? new FrontendSettings { BaseUrl = "http://localhost:5173" };
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

        if (!IsStrongPassword(password))
        {
            return (false, "Password must contain at least 1 uppercase letter, 1 number, and 1 special character", null, false);
        }

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return (false, "Email is already in use.", null, false);
        }

        if (await _userRepository.PhoneExistsAsync(normalizedPhone, cancellationToken: cancellationToken))
        {
            return (false, "Phone is already in use.", null, false);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = normalizedFullName,
            Email = normalizedEmail,
            Phone = normalizedPhone,
            PasswordHash = PasswordHasher.Hash(password),
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
            ExpiresAt = now.AddMinutes(VerificationTokenExpirationMinutes),
            CreatedAt = now
        };

        await _userRepository.AddUserWithWalletAndVerificationTokenAsync(
            user,
            wallet,
            verificationToken,
            cancellationToken);

        // Generate barcode for the new user
        var barcode = await GenerateUniqueBarcodeAsync(user.UserID, cancellationToken);
        user.BarCode = barcode;
        await _userRepository.SaveChangesAsync(cancellationToken);

        await _authEmailService.QueueVerificationAsync(
            user.UserID,
            user.Email,
            "Verify your Cinema Booking account",
            BuildVerificationEmailBody(user.FullName, verificationToken.Token, _frontendSettings.BaseUrl),
            cancellationToken);

        return (true, null, user.UserID, true);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, User? User)> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email.Trim(), cancellationToken);

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash, out var requiresRehash))
        {
            return (false, "Email or password is incorrect.", null);
        }

        if (requiresRehash)
        {
            var upgradedPasswordHash = PasswordHasher.Hash(password);
            var passwordHashUpdated = await _userRepository.TryUpdatePasswordHashAsync(
                user.UserID,
                user.PasswordHash,
                upgradedPasswordHash,
                cancellationToken);

            if (!passwordHashUpdated)
            {
                return (false, "Email or password is incorrect.", null);
            }

            user.PasswordHash = upgradedPasswordHash;
        }

        if (string.Equals(user.Status, LockedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Account is locked. Please contact support.", null);
        }

        if (string.Equals(user.Status, InactiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Your account is currently inactive. Please contact our Administrator for assistance", null);
        }

        if (string.Equals(user.Status, UserStatuses.Unverified, StringComparison.OrdinalIgnoreCase)
            && user.EmailVerifiedAt.HasValue)
        {
            user.Status = ActiveStatus;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        if (string.Equals(user.Status, UserStatuses.Unverified, StringComparison.OrdinalIgnoreCase)
            || !user.EmailVerifiedAt.HasValue)
        {
            return (false, "Please verify your email before logging in.", null);
        }

        if (!string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Account is not active.", null);
        }

        return (true, null, user);
    }

    public async Task<(bool Succeeded, string? Message, bool VerificationEmailSent, int? RetryAfterSeconds)> ResendVerificationEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, EmailNotFoundMessage, false, null);
        }

        var normalizedEmail = email.Trim();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return (false, EmailNotFoundMessage, false, null);
        }

        if (user.EmailVerifiedAt.HasValue)
        {
            return (false, EmailAlreadyVerifiedMessage, false, null);
        }

        var now = DateTime.UtcNow;

        var lastToken = await _userRepository.GetLatestEmailVerificationTokenAsync(
            user.UserID,
            cancellationToken);

        if (lastToken is not null
            && lastToken.CreatedAt > now.AddSeconds(-VerificationEmailResendCooldownSeconds))
        {
            var retryAfterSeconds = GetRetryAfterSeconds(
                lastToken.CreatedAt,
                VerificationEmailResendCooldownSeconds,
                now);

            return (false, $"Please wait {retryAfterSeconds} seconds before requesting another verification email.", false, retryAfterSeconds);
        }

        var verificationToken = new EmailVerificationToken
        {
            Token = GenerateEmailVerificationToken(),
            ExpiresAt = now.AddMinutes(VerificationTokenExpirationMinutes),
            CreatedAt = now,
            UserID = user.UserID
        };

        await _userRepository.ReplaceUnverifiedEmailVerificationTokensAsync(
            user.UserID,
            verificationToken,
            cancellationToken);

        try
        {
            await _authEmailService.QueueVerificationAsync(
                user.UserID,
                user.Email,
                "Verify your Cinema Booking account",
                BuildVerificationEmailBody(user.FullName, verificationToken.Token, _frontendSettings.BaseUrl),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await _userRepository.DeleteEmailVerificationTokenAsync(
                verificationToken.Token,
                cancellationToken);

            return (false, EmailSendFailedMessage, false, null);
        }

        return (true, VerificationEmailSentMessage, true, VerificationEmailResendCooldownSeconds);
    }

    public async Task<(bool Succeeded, string? Message, bool EmailSent, int? RetryAfterSeconds)> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, EmailNotFoundMessage, false, null);
        }

        var normalizedEmail = email.Trim();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return (false, EmailNotFoundMessage, false, null);
        }

        if (!user.EmailVerifiedAt.HasValue)
        {
            return (false, "Email has not been verified.", false, null);
        }

        var now = DateTime.UtcNow;

        var lastToken = await _userRepository.GetLatestPasswordResetTokenAsync(
            user.UserID,
            cancellationToken);

        if (lastToken is not null
            && lastToken.CreatedAt > now.AddSeconds(-ForgotPasswordCooldownSeconds))
        {
            var retryAfterSeconds = GetRetryAfterSeconds(
                lastToken.CreatedAt,
                ForgotPasswordCooldownSeconds,
                now);

            return (false, $"Please wait {retryAfterSeconds} seconds before requesting another password reset email.", false, retryAfterSeconds);
        }

        var resetToken = new PasswordResetToken
        {
            Token = GeneratePasswordResetToken(),
            ExpiresAt = now.AddMinutes(PasswordResetTokenExpirationMinutes),
            CreatedAt = now,
            UserID = user.UserID
        };

        await _userRepository.ReplaceUnusedPasswordResetTokensAsync(
            user.UserID,
            resetToken,
            cancellationToken);

        try
        {
            await _authEmailService.QueuePasswordResetAsync(
                user.UserID,
                user.Email,
                "Reset your Cinema Booking password",
                BuildResetPasswordEmailBody(user.FullName, resetToken.Token, _frontendSettings.BaseUrl),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await _userRepository.DeletePasswordResetTokenAsync(
                resetToken.Token,
                cancellationToken);

            return (false, EmailSendFailedMessage, false, null);
        }

        return (true, PasswordResetEmailSentMessage, true, ForgotPasswordCooldownSeconds);
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
            return (false, "Please enter a new password.");
        }

        if (newPassword.Length < 6)
        {
            return (false, "New password must contain at least 6 characters.");
        }

        if (!IsStrongPassword(newPassword))
        {
            return (false, "Password must contain at least 1 uppercase letter, 1 number, and 1 special character");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return (false, "Confirm password does not match.");
        }

        var now = DateTime.UtcNow;
        var passwordReset = await _userRepository.TryResetPasswordAsync(
            token.Trim(),
            PasswordHasher.Hash(newPassword),
            now,
            cancellationToken);

        if (!passwordReset)
        {
            return (false, "Reset token is invalid, expired, or has already been used");
        }

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> VerifyEmailAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, "Code is required");
        }

        var verificationToken = await _userRepository.GetEmailVerificationTokenAsync(
            code.Trim(),
            cancellationToken);

        if (verificationToken is null)
        {
            return (false, "Code is invalid.");
        }

        if (verificationToken.VerifiedAt.HasValue)
        {
            return (false, "Email has already been verified.");
        }

        var now = DateTime.UtcNow;

        if (verificationToken.ExpiresAt <= now)
        {
            return (false, "Code has expired.");
        }

        if (verificationToken.User is null)
        {
            return (false, "Code is invalid");
        }

        if (verificationToken.User.EmailVerifiedAt.HasValue)
        {
            return (false, "Email is already verified.");
        }

        verificationToken.User.EmailVerifiedAt = now;
        if (string.Equals(
                verificationToken.User.Status,
                UserStatuses.Unverified,
                StringComparison.OrdinalIgnoreCase))
        {
            verificationToken.User.Status = ActiveStatus;
        }
        verificationToken.User.UpdatedAt = now;
        verificationToken.VerifiedAt = now;
        await _userRepository.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    private static bool IsStrongPassword(string password)
    {
        return password.Length >= 6
            && Regex.IsMatch(password, "[A-Z]")
            && Regex.IsMatch(password, @"\d")
            && Regex.IsMatch(password, "[^A-Za-z0-9]");
    }

    private static int GetRetryAfterSeconds(
        DateTime requestedAt,
        int cooldownSeconds,
        DateTime now)
    {
        var availableAt = requestedAt.AddSeconds(cooldownSeconds);
        return Math.Max(1, (int)Math.Ceiling((availableAt - now).TotalSeconds));
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

    private static string BuildVerificationEmailBody(string fullName, string token, string frontendBaseUrl)
    {
        var encodedFullName = WebUtility.HtmlEncode(fullName);
        var encodedToken = WebUtility.HtmlEncode(token);
        var verificationUrl = $"{frontendBaseUrl.TrimEnd('/')}/verify-email";

        return $"""
            <div style="font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #e0e0e0;">

                <div style="background: #c62828; padding: 22px 32px; text-align: center;">
                    <span style="color: #ffffff; font-size: 20px; font-weight: 700; letter-spacing: 1px;">CGV Premium</span>
                </div>

                <div style="padding: 32px 36px 24px;">
                    <p style="margin: 0 0 6px; color: #111111; font-size: 20px; font-weight: 700;">Verify your email</p>
                    <p style="margin: 0 0 24px; color: #555555; font-size: 14px; line-height: 1.6;">
                        Hello <strong>{encodedFullName}</strong>, welcome to CGV Premium!<br />
                        Copy the verification code below and enter it in the verification screen.
                    </p>

                    <div style="text-align: center; margin: 0 0 24px;">
                        <div role="textbox" aria-label="Verification code" title="Select and copy this verification code" style="display: block; background: #f5f5f5; border: 2px dashed #c62828; border-radius: 6px; padding: 16px; color: #222222; font-family: Consolas, 'Courier New', monospace; font-size: 18px; font-weight: 700; letter-spacing: 1px; overflow-wrap: anywhere; cursor: text; user-select: all; -webkit-user-select: all;">
                            {encodedToken}
                        </div>
                        <a href="{verificationUrl}" style="display: inline-block; margin-top: 16px; background: #c62828; color: #ffffff; font-size: 15px; font-weight: 700; text-decoration: none; padding: 14px 40px; border-radius: 6px;">
                            Verify Email
                        </a>
                        <p style="margin: 12px 0 0; font-size: 12px; color: #777777;">Select the code, copy it, and paste it into the verification form.</p>
                        <p style="margin: 6px 0 0; font-size: 12px; color: #999999;">This code expires in {VerificationTokenExpirationMinutes} minutes.</p>
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

    private static string BuildResetPasswordEmailBody(string fullName, string token, string frontendBaseUrl)
    {
        var encodedFullName = WebUtility.HtmlEncode(fullName);
        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/resetPassword?token={Uri.EscapeDataString(token)}";

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
                        <p style="margin: 12px 0 0; font-size: 12px; color: #999999;">This link expires in {PasswordResetTokenExpirationMinutes} minutes</p>
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

    private async Task<string> GenerateUniqueBarcodeAsync(int userId, CancellationToken cancellationToken)
    {
        const string prefix = "CV";
        var barcode = $"{prefix}{userId:D6}";
        
        // Check if barcode already exists (unlikely but for safety)
        while (await _userRepository.BarCodeExistsAsync(barcode, cancellationToken))
        {
            userId++;
            barcode = $"{prefix}{userId:D6}";
        }
        
        return barcode;
    }
}
