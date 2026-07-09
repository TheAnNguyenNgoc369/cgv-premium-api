using System.Security.Claims;
using CinemaBooking.API.Controllers;
using CinemaBooking.Application.AdminUsers;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class AdminUserDeleteControllerTests
{
    [Fact]
    public async Task DeleteUser_Deactivated_ReturnsOkWithSuccessFalse()
    {
        var controller = CreateController(AdminUserResult<AdminUserDeleteResult>.Success(new(false, true)));

        var result = await controller.DeleteUser(2, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(false, ok.Value!.GetType().GetProperty("success")!.GetValue(ok.Value));
        Assert.Equal(true, ok.Value.GetType().GetProperty("deactivated")!.GetValue(ok.Value));
    }

    [Fact]
    public async Task DeleteUser_CloudinaryFailure_ReturnsInternalServerError()
    {
        var controller = CreateController(AdminUserResult<AdminUserDeleteResult>.Failure(
            AdminUserErrorType.Storage,
            "Unable to delete the user's avatar. The user account was not deleted."));

        var result = await controller.DeleteUser(2, CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.StatusCode);
    }

    private static AdminUserController CreateController(
        AdminUserResult<AdminUserDeleteResult> deleteResult)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("userId", "1"),
            new Claim(ClaimTypes.Role, Roles.Admin)
        ], "Test");
        return new AdminUserController(new StubAdminUserService(deleteResult))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private sealed class StubAdminUserService : IAdminUserService
    {
        private readonly AdminUserResult<AdminUserDeleteResult> _deleteResult;
        public StubAdminUserService(AdminUserResult<AdminUserDeleteResult> deleteResult) =>
            _deleteResult = deleteResult;

        public Task<AdminUserResult<AdminUserDeleteResult>> DeleteAsync(int adminId, int userId,
            string? ipAddress, CancellationToken cancellationToken = default) => Task.FromResult(_deleteResult);
        public Task<AdminUserResult<AdminUserPageResult>> GetUsersAsync(string? search, string? role,
            string? status, int page, int pageSize, CancellationToken cancellationToken = default) => Unsupported<AdminUserPageResult>();
        public Task<AdminUserResult<User>> CreateAsync(int adminId, AdminUserCreateCommand command,
            string? ipAddress, CancellationToken cancellationToken = default) => Unsupported<User>();
        public Task<AdminUserResult<User>> UpdateAsync(int adminId, int userId,
            AdminUserUpdateCommand command, string? ipAddress, CancellationToken cancellationToken = default) => Unsupported<User>();
        public Task<AdminUserResult<User>> ChangeRoleAsync(int adminId, int userId, string role,
            int? cinemaId, string? ipAddress, CancellationToken cancellationToken = default) => Unsupported<User>();
        public Task<AdminUserResult<User>> ChangeStatusAsync(int adminId, int userId, string status,
            string? ipAddress, CancellationToken cancellationToken = default) => Unsupported<User>();
        public Task<AdminUserResult<User>> ResetPasswordAsync(int adminId, int userId, string password,
            string? ipAddress, CancellationToken cancellationToken = default) => Unsupported<User>();
        public Task<AdminUserResult<User>> UploadAvatarAsync(int adminId, int userId, Stream imageStream,
            string fileName, string? contentType, long fileSize, string? ipAddress,
            CancellationToken cancellationToken = default) => Unsupported<User>();
        public Task<AdminUserResult<User>> DeleteAvatarAsync(int adminId, int userId,
            string? ipAddress, CancellationToken cancellationToken = default) => Unsupported<User>();

        private static Task<AdminUserResult<T>> Unsupported<T>() => throw new NotSupportedException();
    }
}
