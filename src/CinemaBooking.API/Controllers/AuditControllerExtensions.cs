using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

internal static class AuditControllerExtensions
{
    public static bool TryAuditActorId(this ControllerBase controller, out int actorId)
    {
        var userIdValue = controller.User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out actorId);
    }

    public static string AuditIpAddress(this ControllerBase controller) =>
        controller.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
