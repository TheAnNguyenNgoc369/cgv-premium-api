using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using CinemaBooking.API.Configuration;
using CinemaBooking.API.Contracts.Auth;
using CinemaBooking.API.Contracts.Cinemas;
using CinemaBooking.API.Services;
using CinemaBooking.Application.Authentication;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private static readonly TimeSpan EnumerationSafeMinimumResponseDuration = TimeSpan.FromMilliseconds(500);

    private readonly IAuthService _authService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IAuthRequestRateLimiter _authRequestRateLimiter;

    public AuthController(
        IAuthService authService,
        JwtTokenService jwtTokenService,
        ITokenRevocationService tokenRevocationService,
        IAuthRequestRateLimiter authRequestRateLimiter)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
        _tokenRevocationService = tokenRevocationService;
        _authRequestRateLimiter = authRequestRateLimiter;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicyNames.Register)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        if (!await TryAcquireEmailLimitAsync("register", model.Email, cancellationToken))
        {
            return TooManyAuthRequests();
        }

        var result = await _authService.RegisterAsync(
            model.FullName,
            model.Email,
            model.Phone,
            model.Password,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new
        {
            success = true,
            message = "Registration successful.",
            userId = result.UserId,
            verificationEmailSent = result.VerificationEmailSent
        });
    }

    [HttpPost("resend-verification-email")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicyNames.EmailAction)]
    public async Task<IActionResult> ResendVerificationEmail(
        [FromBody] ResendVerificationEmailRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var startedAt = Stopwatch.GetTimestamp();

        if (!await TryAcquireEmailLimitAsync("resend-verification-email", model.Email, cancellationToken))
        {
            await EnsureMinimumResponseDurationAsync(startedAt, cancellationToken);
            return TooManyAuthRequests();
        }

        var result = await _authService.ResendVerificationEmailAsync(
            model.Email,
            cancellationToken);

        await EnsureMinimumResponseDurationAsync(startedAt, cancellationToken);

        return Ok(new
        {
            success = result.Succeeded,
            message = result.Message,
            verificationEmailSent = result.VerificationEmailSent,
            retryAfterSeconds = result.RetryAfterSeconds
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicyNames.EmailAction)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var startedAt = Stopwatch.GetTimestamp();

        if (!await TryAcquireEmailLimitAsync("forgot-password", model.Email, cancellationToken))
        {
            await EnsureMinimumResponseDurationAsync(startedAt, cancellationToken);
            return TooManyAuthRequests();
        }

        var result = await _authService.ForgotPasswordAsync(
            model.Email,
            cancellationToken);

        await EnsureMinimumResponseDurationAsync(startedAt, cancellationToken);

        return Ok(new
        {
            success = result.Succeeded,
            message = result.Message,
            emailSent = result.EmailSent,
            retryAfterSeconds = result.RetryAfterSeconds
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var result = await _authService.ResetPasswordAsync(
            model.Token,
            model.NewPassword,
            model.ConfirmPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = "Password has been reset successfully."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicyNames.Login)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        if (!await TryAcquireEmailLimitAsync("login", model.Email, cancellationToken))
        {
            return TooManyAuthRequests();
        }

        var result = await _authService.LoginAsync(model.Email, model.Password, cancellationToken);

        if (!result.Succeeded || result.User is null)
        {
            return Unauthorized(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        var token = _jwtTokenService.GenerateToken(result.User);

        return Ok(new
        {
            success = true,
            message = "Login successful.",
            token,
            user = new
            {
                result.User.UserID,
                result.User.FullName,
                result.User.Email,
                result.User.Role,
                result.User.Status,
                result.User.AvatarURL,
                cinema = result.User.Role is Roles.Manager or Roles.Staff && result.User.Cinema is not null
                    ? new CinemaSummaryResponse(
                        result.User.Cinema.CinemaID,
                        result.User.Cinema.CinemaName,
                        result.User.Cinema.Address,
                        result.User.Cinema.Latitude.HasValue ? decimal.ToDouble(result.User.Cinema.Latitude.Value) : null,
                        result.User.Cinema.Longitude.HasValue ? decimal.ToDouble(result.User.Cinema.Longitude.Value) : null,
                        EnumValueMapper.ToApiValue(result.User.Cinema.Status))
                    : null
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!TryGetBearerToken(out var token))
        {
            return BadRequest(new { success = false, message = "Bearer token is required" });
        }

        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            await _tokenRevocationService.RevokeAsync(
                token,
                jwtToken.ValidTo,
                cancellationToken);
        }
        catch (ArgumentException)
        {
            return BadRequest(new { success = false, message = "Bearer token is invalid" });
        }

        return Ok(new { success = true, message = "Logout successful" });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicyNames.Verify)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var result = await _authService.VerifyEmailAsync(model.Code, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, message = "Email verified successfully" });
    }

    private bool TryGetBearerToken(out string token)
    {
        token = string.Empty;

        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return false;
        }

        var authorizationValue = authorizationHeader.ToString();
        const string bearerPrefix = "Bearer ";

        if (!authorizationValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = authorizationValue[bearerPrefix.Length..].Trim();
        return token.Length > 0;
    }

    private static async Task EnsureMinimumResponseDurationAsync(
        long startedAt,
        CancellationToken cancellationToken)
    {
        var remaining = EnumerationSafeMinimumResponseDuration - Stopwatch.GetElapsedTime(startedAt);

        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, cancellationToken);
        }
    }

    private async ValueTask<bool> TryAcquireEmailLimitAsync(
        string action,
        string email,
        CancellationToken cancellationToken)
    {
        return await _authRequestRateLimiter.TryAcquireAsync(action, email, cancellationToken);
    }

    private static IActionResult TooManyAuthRequests()
    {
        return new ObjectResult(new
        {
            success = false,
            message = "Too many requests. Please try again later."
        })
        {
            StatusCode = StatusCodes.Status429TooManyRequests
        };
    }
}
