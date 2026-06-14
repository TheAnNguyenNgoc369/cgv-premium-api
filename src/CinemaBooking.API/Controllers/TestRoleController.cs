using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/test-role")]
public sealed class TestRoleController : ControllerBase
{
    [HttpGet("customer")]
    [Authorize(Roles = Roles.Customer)]
    public IActionResult CustomerOnly()
    {
        return Ok(new { message = "Bạn là Customer ✅" });
    }

    [HttpGet("staff")]
    [Authorize(Roles = Roles.Staff)]
    public IActionResult StaffOnly()
    {
        return Ok(new { message = "Bạn là Staff ✅" });
    }

    [HttpGet("manager")]
    [Authorize(Roles = Roles.Manager)]
    public IActionResult ManagerOnly()
    {
        return Ok(new { message = "Bạn là Manager ✅" });
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "Bạn là Admin ✅" });
    }

    [HttpGet("customer-or-staff")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public IActionResult CustomerOrStaff()
    {
        return Ok(new { message = "Bạn là Customer hoặc Staff ✅" });
    }
}
