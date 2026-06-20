using CinemaBooking.API.Contracts.Images;
using CinemaBooking.API.Contracts.Users;
using CinemaBooking.Application.Users;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
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

        return Ok(ToProfileResponse(user));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
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
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProfileResponse(result.User!));
    }

    [HttpPut("/api/users/profile/avatar")]
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
            return BadRequest(new { message = "Image file is required" });
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
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            secureUrl = result.User!.AvatarURL,
            publicId = result.User.AvatarPublicId,
            user = ToProfileResponse(result.User)
        });
    }

    [HttpDelete("/api/users/profile/avatar")]
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
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProfileResponse(result.User!));
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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteUser(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.DeleteAsync(id, cancellationToken);

        if (result.Succeeded)
        {
            return NoContent();
        }

        if (result.ErrorMessage == "User not found")
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Conflict(new { message = result.ErrorMessage });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdValue = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out userId);
    }

    private static object ToProfileResponse(User user)
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
            user.CreatedAt
        };
    }
}
