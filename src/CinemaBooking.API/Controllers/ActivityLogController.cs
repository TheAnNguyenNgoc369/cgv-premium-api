using CinemaBooking.Application.ActivityLogs;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/admin/activity-logs")]
[Authorize(Roles = Roles.Admin)]
public sealed class ActivityLogController : ControllerBase
{
    private readonly IActivityLogService _service;
    public ActivityLogController(IActivityLogService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? actionType, [FromQuery] string? module,
        [FromQuery] int? actorId, [FromQuery] int? targetUserId, [FromQuery] string? targetTable,
        [FromQuery] int? targetId, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return BadRequest(new { success = false, message = "Page must be at least 1 and pageSize must be between 1 and 100." });
        if (startDate > endDate)
            return BadRequest(new { success = false, message = "Start date must not be later than end date." });
        if (actionType is not null && !_service.GetActionTypes().Contains(actionType))
            return BadRequest(new { success = false, message = "Invalid actionType." });
        return Ok(await _service.GetAsync(actionType,module,actorId,targetUserId,targetTable,targetId,startDate,endDate,page,pageSize,cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var log = await _service.GetByIdAsync(id, cancellationToken);
        return log is null ? NotFound(new { success = false, message = "Activity log not found." }) : Ok(log);
    }

    [HttpGet("action-types")]
    public IActionResult GetActionTypes() => Ok(_service.GetActionTypes());
}
