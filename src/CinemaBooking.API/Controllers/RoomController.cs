using CinemaBooking.API.Contracts.Rooms;
using CinemaBooking.Application.Rooms;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly IManagerCinemaScopeService _managerCinemaScopeService;

    public RoomController(IRoomService roomService, IManagerCinemaScopeService managerCinemaScopeService)
    {
        _roomService = roomService;
        _managerCinemaScopeService = managerCinemaScopeService;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Customer + "," + Roles.Staff + "," + Roles.Admin + "," + Roles.Manager)]
    public async Task<IActionResult> GetRooms(CancellationToken cancellationToken)
    {
        var rooms = await _roomService.GetRoomsAsync(cancellationToken);

        return Ok(rooms.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Customer + "," + Roles.Staff + "," + Roles.Admin + "," + Roles.Manager)]
    public async Task<IActionResult> GetRoomById(
        int id,
        CancellationToken cancellationToken)
    {
        var room = await _roomService.GetRoomByIdAsync(id, cancellationToken);

        if (room is null)
        {
            return NotFound(new { success = false, message = "Room not found" });
        }

        return Ok(ToResponse(room));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> CreateRoom(
        [FromBody] RoomRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _roomService.CreateRoomAsync(
            request.CinemaId,
            request.Name,
            request.Type,
            request.Status,
            request.Description,
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapError(result.ErrorMessage);
        }

        var response = ToResponse(result.Room!);

        return CreatedAtAction(
            nameof(GetRoomById),
            new { id = response.RoomId },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> UpdateRoom(
        int id,
        [FromBody] RoomRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _roomService.UpdateRoomAsync(
            id,
            request.CinemaId,
            request.Name,
            request.Type,
            request.Status,
            request.Description,
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Room not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return MapError(result.ErrorMessage);
        }

        return Ok(ToResponse(result.Room!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> DeleteRoom(
        int id,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _roomService.DeleteRoomAsync(id, managerCinemaId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Room not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            if (result.ErrorMessage == CinemaScopeMessages.AccessDenied)
                return CinemaScopeForbidden();

            return Conflict(new { success = false, message = result.ErrorMessage });
        }

        return NoContent();
    }

    private static RoomResponse ToResponse(Room room)
    {
        return new RoomResponse(
            room.RoomID,
            room.CinemaID,
            room.RoomName,
            EnumValueMapper.ToApiValue(room.RoomType == "3D" ? "THREE_D" : room.RoomType),
            room.Capacity,
            EnumValueMapper.ToApiValue(room.Status),
            room.Description,
            room.CreatedAt);
    }

    private async Task<int?> GetManagerCinemaIdAsync(CancellationToken cancellationToken) =>
        int.TryParse(User.FindFirst("userId")?.Value, out var userId)
            ? await _managerCinemaScopeService.GetAssignedCinemaIdAsync(userId, cancellationToken)
            : null;

    private IActionResult MapError(string? message) =>
        message == CinemaScopeMessages.AccessDenied
            ? CinemaScopeForbidden()
            : BadRequest(new { success = false, message });

    private ObjectResult CinemaScopeForbidden() => StatusCode(
        StatusCodes.Status403Forbidden,
        new { success = false, message = CinemaScopeMessages.AccessDenied });
}
