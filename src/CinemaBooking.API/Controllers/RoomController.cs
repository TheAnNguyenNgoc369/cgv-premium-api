using CinemaBooking.API.Contracts.Rooms;
using CinemaBooking.Application.Rooms;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize(Roles = Roles.Manager)]
public sealed class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRooms(CancellationToken cancellationToken)
    {
        var rooms = await _roomService.GetRoomsAsync(cancellationToken);

        return Ok(rooms.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
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
    public async Task<IActionResult> CreateRoom(
        [FromBody] RoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roomService.CreateRoomAsync(
            request.CinemaId,
            request.Name,
            request.Type,
            request.Status,
            request.Description,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = ToResponse(result.Room!);

        return CreatedAtAction(
            nameof(GetRoomById),
            new { id = response.RoomId },
            response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRoom(
        int id,
        [FromBody] RoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roomService.UpdateRoomAsync(
            id,
            request.CinemaId,
            request.Name,
            request.Type,
            request.Status,
            request.Description,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Room not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(ToResponse(result.Room!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRoom(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _roomService.DeleteRoomAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Room not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

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
}
