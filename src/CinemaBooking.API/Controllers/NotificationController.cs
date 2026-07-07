using CinemaBooking.Application.Notifications;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Shared.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController, Route("api/notifications"), Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
public sealed class NotificationController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int page = 1, int pageSize = 10, bool? isRead = null, string? type = null,
        DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    { try { var x = await service.GetAsync(UserId(), page, pageSize, isRead, type, fromDate, toDate, ct); return Ok(new { items = x.Items.Select(Map), x.Page, x.PageSize, x.TotalItems, totalPages = (int)Math.Ceiling(x.TotalItems / (double)x.PageSize) }); } catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); } }
    [HttpGet("unread-count")]
    public async Task<IActionResult> Count(CancellationToken ct) => Ok(new { count = await service.GetUnreadCountAsync(UserId(), ct) });
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var x = await service.GetByIdAsync(UserId(), id, ct);
        if (x.ErrorType == "forbidden") return StatusCode(403, new { success = false, message = "Forbidden." });
        return x.Notification is null
            ? NotFound(new { success = false, message = "Notification not found." })
            : Ok(Map(x.Notification));
    }
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> Read(int id, CancellationToken ct) => Mutation(await service.MarkReadAsync(UserId(), id, ct), "Notification marked as read.");
    [HttpPut("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct) { var count = await service.MarkAllReadAsync(UserId(), ct); return Ok(new { success = true, updatedCount = count, message = "All notifications marked as read." }); }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { var x = await service.DeleteAsync(UserId(), id, ct); return x.Succeeded ? NoContent() : Mutation(x, ""); }
    [HttpDelete("read")]
    public async Task<IActionResult> DeleteRead(CancellationToken ct) { var count = await service.DeleteReadAsync(UserId(), ct); return Ok(new { success = true, deletedCount = count, message = "Read notifications deleted successfully." }); }
    private IActionResult Mutation(NotificationMutationResult x, string message) => x.Succeeded ? Ok(new { success = true, message }) : x.ErrorType == "forbidden" ? StatusCode(403, new { success = false, message = x.Error }) : NotFound(new { success = false, message = x.Error });
    private int UserId() => int.Parse(User.FindFirst("userId")!.Value);
    private static object Map(NotificationItem x) => new { x.NotificationId, x.Title, x.Message, x.Type, x.EventType, x.ReferenceType, x.ReferenceId, x.ActionUrl, x.IsRead, readAt = x.ReadAt.HasValue ? VietnamTime.FromUtc(x.ReadAt.Value) : (DateTimeOffset?)null, createdAt = VietnamTime.FromUtc(x.CreatedAt) };
}
