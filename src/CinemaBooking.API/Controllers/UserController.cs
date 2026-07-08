using CinemaBooking.API.Contracts.Images;
using CinemaBooking.API.Contracts.Users;
using CinemaBooking.API.Contracts.Cinemas;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Application.Membership;
using CinemaBooking.Application.Users;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMembershipService _membershipService;

    public UserController(
        IUserService userService,
        IMembershipService membershipService)
    {
        _userService = userService;
        _membershipService = membershipService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetProfileAsync(userId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var membership = await _membershipService.GetMyMembershipAsync(userId, cancellationToken);

        return Ok(ToProfileResponseWithCinema(user, membership.TotalRefunds, membership.UsedRefunds));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _userService.UpdateProfileAsync(
            userId,
            model.FullName,
            model.Phone,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "User not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var membership = await _membershipService.GetMyMembershipAsync(userId, cancellationToken);

        return Ok(ToProfileResponse(result.User!, membership.TotalRefunds, membership.UsedRefunds));
    }

    [HttpPut("profile/avatar")]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] ImageUploadRequest model,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (model.File is null)
        {
            return BadRequest(new { success = false, message = "Image file is required" });
        }

        await using var stream = model.File.OpenReadStream();
        var result = await _userService.UploadAvatarAsync(
            userId,
            stream,
            model.File.FileName,
            model.File.ContentType,
            model.File.Length,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "User not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var membership = await _membershipService.GetMyMembershipAsync(userId, cancellationToken);

        return Ok(new
        {
            secureUrl = result.User!.AvatarURL,
            publicId = result.User.AvatarPublicId,
            user = ToProfileResponse(result.User, membership.TotalRefunds, membership.UsedRefunds)
        });
    }

    [HttpDelete("profile/avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _userService.DeleteAvatarAsync(userId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "User not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var wallet = await _userService.GetWalletAsync(userId, cancellationToken);

        if (wallet is null)
        {
            return Ok(null);
        }

        return Ok(new
        {
            wallet.WalletID,
            wallet.Balance
        });
    }

    [HttpGet("~/api/users/lookup")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? email,
        [FromQuery] string? phone,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if (normalizedEmail is null && normalizedPhone is null)
        {
            return BadRequest(new { success = false, message = "email or phone is required." });
        }

        if (normalizedEmail is not null && !new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return BadRequest(new { success = false, message = "email is invalid." });
        }

        if (normalizedPhone is not null && !Regex.IsMatch(normalizedPhone, @"^0[0-9]{9}$"))
        {
            return BadRequest(new { success = false, message = "phone must contain 10 digits and start with 0." });
        }

        var user = await _userService.LookupCustomerAsync(normalizedEmail, normalizedPhone, cancellationToken);

        if (user is null)
        {
            return NotFound(new { success = false, message = "User not found." });
        }

        var membership = await _membershipService.GetMyMembershipAsync(user.UserID, cancellationToken);

        return Ok(ToLookupResponse(user, membership));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid authenticated user." });
        }

        var result = await _userService.ChangePasswordAsync(
            userId,
            model.OldPassword,
            model.NewPassword,
            model.ConfirmPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, message = "Password changed successfully." });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdValue = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out userId);
    }

    private static object ToProfileResponse(
        User user,
        int totalRefunds,
        int usedRefunds)
    {
        return new
        {
            user.UserID,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.AvatarURL,
            user.TotalPoints,
            total_refunds = totalRefunds,
            used_refunds = usedRefunds,
            user.CreatedAt
        };
    }

    private static object ToProfileResponseWithCinema(
        User user,
        int totalRefunds,
        int usedRefunds)
    {
        return new
        {
            user.UserID,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.AvatarURL,
            user.TotalPoints,
            total_refunds = totalRefunds,
            used_refunds = usedRefunds,
            user.CreatedAt,
            cinema = user.Role is Roles.Manager or Roles.Staff && user.Cinema is not null
                ? new CinemaSummaryResponse(
                    user.Cinema.CinemaID,
                    user.Cinema.CinemaName,
                    user.Cinema.Address,
                    user.Cinema.Latitude.HasValue ? decimal.ToDouble(user.Cinema.Latitude.Value) : null,
                    user.Cinema.Longitude.HasValue ? decimal.ToDouble(user.Cinema.Longitude.Value) : null,
                    EnumValueMapper.ToApiValue(user.Cinema.Status))
                : null
        };
    }

    private static UserLookupResponse ToLookupResponse(
        User user,
        MembershipInfo membership)
    {
        return new UserLookupResponse(
            true,
            "User found.",
            new UserLookupResponse.UserInfo(
                user.UserID,
                user.FullName,
                user.Email,
                user.Phone,
                user.Role,
                user.Status,
                user.AvatarURL,
                new UserLookupResponse.UserMembershipInfo(
                    membership.CurrentTier,
                    membership.TotalPoints,
                    membership.DiscountPercent,
                    membership.NextTier,
                    membership.PointsToNextTier),
                user.Wallet is null
                    ? null
                    : new UserLookupResponse.UserWalletInfo(user.Wallet.WalletID, user.Wallet.Balance),
                null));
    }
}
