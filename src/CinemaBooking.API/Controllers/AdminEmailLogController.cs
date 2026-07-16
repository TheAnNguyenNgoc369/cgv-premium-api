using CinemaBooking.Application.Notifications;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/admin/email-logs")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminEmailLogController : ControllerBase
{
    private readonly IEmailLogService _service;

    public AdminEmailLogController(IEmailLogService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? userId, [FromQuery] string? recipientEmail,
        [FromQuery] string? eventType, [FromQuery] string? deliveryStatus, [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.GetAsync(userId, recipientEmail, eventType, deliveryStatus,
                fromDate, toDate, page, pageSize, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
