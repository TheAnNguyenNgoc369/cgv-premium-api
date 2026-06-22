using System.IdentityModel.Tokens.Jwt;
using CinemaBooking.API.Contracts.Auth;
using CinemaBooking.API.Services;
using CinemaBooking.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ITokenRevocationService _tokenRevocationService;

    public AuthController(
        IAuthService authService,
        JwtTokenService jwtTokenService,
        ITokenRevocationService tokenRevocationService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
        _tokenRevocationService = tokenRevocationService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(
            model.FullName,
            model.Email,
            model.Phone,
            model.Password,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            message = "Đăng ký thành công",
            userId = result.UserId,
            verificationEmailSent = result.VerificationEmailSent
        });
    }

    [HttpPost("resend-verification-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerificationEmail(
        [FromBody] ResendVerificationEmailRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.ResendVerificationEmailAsync(
            model.Email,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage,
                verificationEmailSent = result.VerificationEmailSent
            });
        }

        return Ok(new
        {
            message = "Verification email sent successfully",
            verificationEmailSent = result.VerificationEmailSent
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.ForgotPasswordAsync(
            model.Email,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Ok(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = "Email has been sent successfully. Please check your email."
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
            return BadRequest(ModelState);
        }

        var result = await _authService.ResetPasswordAsync(
            model.Token,
            model.NewPassword,
            model.ConfirmPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Ok(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = "Email has been sent successfully. Please check your email."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
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
            message = "Đăng nhập thành công",
            token,
            user = new
            {
                result.User.UserID,
                result.User.FullName,
                result.User.Email,
                result.User.Role,
                result.User.Status,
                result.User.AvatarURL
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        if (!TryGetBearerToken(out var token))
        {
            return BadRequest(new { message = "Bearer token is required" });
        }

        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            _tokenRevocationService.Revoke(token, jwtToken.ValidTo);
        }
        catch (ArgumentException)
        {
            return BadRequest(new { message = "Bearer token is invalid" });
        }

        return Ok(new { message = "Logout successful" });
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyEmailAsync(token, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Email verified successfully" });
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
}
