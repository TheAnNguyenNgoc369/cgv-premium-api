using CinemaBooking.API.Contracts.RoomTypes;
using CinemaBooking.Application.ActivityLogs;
using CinemaBooking.Application.RoomTypes;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController, Route("api/room-types"), Authorize]
public sealed class RoomTypeController(IRoomTypeService service, IActivityLogService activityLogs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok((await service.GetAllAsync(ct)).Select(ToResponse));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    { var value = await service.GetByIdAsync(id, ct); return value is null ? NotFound(new { success = false, message = "Room type not found." }) : Ok(ToResponse(value)); }

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(RoomTypeRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request.TypeName, request.ExtraPrice, request.Description, ct);
        if (!result.Succeeded) return BadRequest(new { success = false, message = result.ErrorMessage });
        await activityLogs.RecordAsync(this.AuditActorId(), AdminActionTypes.CreateRoomType, "RoomType", result.RoomType!.RoomTypeID, $"Created room type {result.RoomType.RoomTypeID}", this.AuditIpAddress(), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.RoomType.RoomTypeID }, ToResponse(result.RoomType));
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, RoomTypeRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request.TypeName, request.ExtraPrice, request.Description, ct);
        if (!result.Succeeded) return result.ErrorMessage == "Room type not found." ? NotFound(new { success = false, message = result.ErrorMessage }) : BadRequest(new { success = false, message = result.ErrorMessage });
        await activityLogs.RecordAsync(this.AuditActorId(), AdminActionTypes.UpdateRoomType, "RoomType", id, $"Updated room type {id}", this.AuditIpAddress(), ct);
        return Ok(ToResponse(result.RoomType!));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.Succeeded) return result.ErrorMessage == "Room type not found." ? NotFound(new { success = false, message = result.ErrorMessage }) : Conflict(new { success = false, message = result.ErrorMessage });
        await activityLogs.RecordAsync(this.AuditActorId(), AdminActionTypes.DeleteRoomType, "RoomType", id, $"Deleted room type {id}", this.AuditIpAddress(), ct);
        return NoContent();
    }

    private static RoomTypeResponse ToResponse(RoomType value) => new(value.RoomTypeID, value.TypeName, value.ExtraPrice, value.Description);
}
