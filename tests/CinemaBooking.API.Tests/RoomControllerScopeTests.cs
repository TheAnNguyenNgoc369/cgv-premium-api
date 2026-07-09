using System.Security.Claims;
using CinemaBooking.API.Controllers;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Application.Rooms;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class RoomControllerScopeTests
{
    [Fact]
    public async Task GetRoomById_ManagerRequestsAnotherCinema_ReturnsForbidden()
    {
        var controller = new RoomController(
            new StubRoomService(new Room { RoomID = 10, CinemaID = 2 }),
            new StubManagerCinemaScopeService(1))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("userId", "7"),
                        new Claim(ClaimTypes.Role, Roles.Manager)
                    ], "test"))
                }
            }
        };

        var result = await controller.GetRoomById(10, default);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    private sealed class StubManagerCinemaScopeService(int? cinemaId) : IManagerCinemaScopeService
    {
        public Task<int?> GetAssignedCinemaIdAsync(
            int userId, CancellationToken cancellationToken = default) => Task.FromResult(cinemaId);
    }

    private sealed class StubRoomService(Room room) : IRoomService
    {
        public Task<List<Room>> GetRoomsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Room>> GetRoomsByCinemaIdAsync(int cinemaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken cancellationToken = default) => Task.FromResult<Room?>(room);
        public Task<(bool Succeeded, string? ErrorMessage, Room? Room)> CreateRoomAsync(int cinemaId, string name, int roomTypeId, string status, string? description, int? managerCinemaId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Succeeded, string? ErrorMessage, Room? Room)> UpdateRoomAsync(int roomId, int cinemaId, string name, int roomTypeId, string status, string? description, int? managerCinemaId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Succeeded, string? ErrorMessage)> DeleteRoomAsync(int roomId, int? managerCinemaId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
