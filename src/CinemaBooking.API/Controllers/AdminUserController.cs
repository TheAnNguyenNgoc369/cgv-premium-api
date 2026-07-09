using CinemaBooking.API.Contracts.AdminUsers;
using CinemaBooking.API.Contracts.Images;
using CinemaBooking.Application.AdminUsers;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminUserController : ControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUserController(IAdminUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetUsersAsync(
            search, role, status, page, pageSize, cancellationToken);
        if (!result.Succeeded) return MapError(result.ErrorType, result.ErrorMessage);
        var data = result.Value!;
        return Ok(new PagedAdminUserResponse(
            data.Items.Select(ToListResponse).ToList(), data.Page, data.PageSize,
            data.TotalItems, (int)Math.Ceiling(data.TotalItems / (double)data.PageSize)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var command = new AdminUserCreateCommand(
            request.FullName, request.Email, request.Phone, request.Password,
            request.Role, request.Status, request.CinemaId);
        var result = await _service.CreateAsync(
            adminId, command, GetIpAddress(), cancellationToken);
        if (!result.Succeeded) return MapError(result.ErrorType, result.ErrorMessage);
        var response = ToResponse(result.Value!);
        return Created($"/api/admin/users/{response.UserId}", response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(
        int id, UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var result = await _service.UpdateAsync(adminId, id,
            new AdminUserUpdateCommand(request.FullName, request.Email, request.Phone, request.CinemaId),
            GetIpAddress(), cancellationToken);
        return ToUserResult(result);
    }

    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(
        int id, ChangeUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var result = await _service.ChangeRoleAsync(
            adminId, id, request.Role, request.CinemaId, GetIpAddress(), cancellationToken);
        return ToUserResult(result);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int id, ChangeUserStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var result = await _service.ChangeStatusAsync(
            adminId, id, request.Status, GetIpAddress(), cancellationToken);
        return ToUserResult(result);
    }

    [HttpPatch("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(
        int id, ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var result = await _service.ResetPasswordAsync(
            adminId, id, request.Password, GetIpAddress(), cancellationToken);
        if (!result.Succeeded) return MapError(result.ErrorType, result.ErrorMessage);
        return Ok(new { success = true, message = "Password reset successfully" });
    }

    [HttpPut("{id:int}/avatar")]
    public async Task<IActionResult> UploadAvatar(
        int id, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        if (request.File is null)
            return BadRequest(new { success = false, message = "Image file is required" });
        await using var stream = request.File.OpenReadStream();
        var result = await _service.UploadAvatarAsync(
            adminId, id, stream, request.File.FileName, request.File.ContentType,
            request.File.Length, GetIpAddress(), cancellationToken);
        return ToUserResult(result);
    }

    [HttpDelete("{id:int}/avatar")]
    public async Task<IActionResult> DeleteAvatar(int id, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var result = await _service.DeleteAvatarAsync(
            adminId, id, GetIpAddress(), cancellationToken);
        return ToUserResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var result = await _service.DeleteAsync(adminId, id, GetIpAddress(), cancellationToken);
        if (!result.Succeeded) return MapError(result.ErrorType, result.ErrorMessage);
        if (result.Value!.PhysicallyDeleted) return NoContent();
        return Ok(new
        {
            success = false,
            message = "User could not be physically deleted and was deactivated",
            deactivated = true
        });
    }

    private IActionResult ToUserResult(AdminUserResult<User> result) =>
        result.Succeeded ? Ok(ToResponse(result.Value!)) : MapError(result.ErrorType, result.ErrorMessage);

    private IActionResult MapError(AdminUserErrorType errorType, string? message) => errorType switch
    {
        AdminUserErrorType.NotFound => NotFound(new { success = false, message }),
        AdminUserErrorType.Conflict => Conflict(new { success = false, message }),
        AdminUserErrorType.Forbidden => StatusCode(
            StatusCodes.Status403Forbidden, new { success = false, message }),
        AdminUserErrorType.Storage => StatusCode(
            StatusCodes.Status500InternalServerError, new { success = false, message }),
        _ => BadRequest(new { success = false, message })
    };

    private bool TryGetAdminId(out int adminId) =>
        int.TryParse(User.FindFirst("userId")?.Value, out adminId);
    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static AdminUserResponse ToResponse(User user) => new(
        user.UserID, user.FullName, user.Email, user.Phone!, user.Role, user.Status,
        user.CinemaID, user.AvatarURL, user.TotalPoints, user.CreatedAt, user.UpdatedAt);

    private static AdminUserListResponse ToListResponse(User user) => new(
        user.UserID, user.FullName, user.Email, user.Phone!, user.Role, user.Status,
        user.CinemaID, user.AvatarURL, user.TotalPoints, user.LoyaltyTier?.TierName,
        user.CreatedAt, user.UpdatedAt);
}
