using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

internal static class AuditControllerExtensions
{
    public static int AuditActorId(this ControllerBase controller) =>
        int.Parse(controller.User.FindFirst("userId")!.Value);

    public static string AuditIpAddress(this ControllerBase controller) =>
        controller.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
