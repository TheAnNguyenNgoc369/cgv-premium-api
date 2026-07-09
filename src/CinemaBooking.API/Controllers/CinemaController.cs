using CinemaBooking.API.Contracts.Cinemas;
using CinemaBooking.Application.Cinemas;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.Application.ActivityLogs;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/cinemas")]
public sealed class CinemaController : ControllerBase
{
    private readonly ICinemaService _cinemaService;
    private readonly IActivityLogService _activityLogs;

    public CinemaController(ICinemaService cinemaService, IActivityLogService activityLogs)
    {
        _cinemaService = cinemaService;
        _activityLogs = activityLogs;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCinemas(CancellationToken cancellationToken)
    {
        var cinemas = await _cinemaService.GetCinemasAsync(cancellationToken);

        var response = cinemas.Select(ToResponse);

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCinemaById(
        int id,
        CancellationToken cancellationToken)
    {
        var cinema = await _cinemaService.GetCinemaByIdAsync(id, cancellationToken);

        if (cinema is null)
        {
            return NotFound(new { success = false, message = "Cinema not found" });
        }

        return Ok(ToResponse(cinema));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateCinema(
        [FromBody] CinemaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cinemaService.CreateCinemaAsync(
            request.CinemaName,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = ToResponse(result.Cinema!);
        await _activityLogs.RecordAsync(this.AuditActorId(), AdminActionTypes.CreateCinema,
            "Cinema", response.CinemaId, $"Created cinema {response.CinemaId}", this.AuditIpAddress(), cancellationToken);

        return CreatedAtAction(
            nameof(GetCinemaById),
            new { id = response.CinemaId },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateCinema(
        int id,
        [FromBody] CinemaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cinemaService.UpdateCinemaAsync(
            id,
            request.CinemaName,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Cinema not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        await _activityLogs.RecordAsync(this.AuditActorId(), AdminActionTypes.UpdateCinema,
            "Cinema", id, $"Updated cinema {id}", this.AuditIpAddress(), cancellationToken);
        return Ok(ToResponse(result.Cinema!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteCinema(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _cinemaService.DeleteCinemaAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Cinema not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return Conflict(new { success = false, message = result.ErrorMessage });
        }

        await _activityLogs.RecordAsync(this.AuditActorId(), AdminActionTypes.DeleteCinema,
            "Cinema", id, $"Deleted cinema {id}", this.AuditIpAddress(), cancellationToken);
        return NoContent();
    }

    private static CinemaResponse ToResponse(Cinema cinema)
    {
        return new CinemaResponse(
            cinema.CinemaID,
            cinema.CinemaName,
            cinema.Address,
            cinema.Latitude.HasValue ? decimal.ToDouble(cinema.Latitude.Value) : null,
            cinema.Longitude.HasValue ? decimal.ToDouble(cinema.Longitude.Value) : null,
            EnumValueMapper.ToApiValue(cinema.Status),
            cinema.CreatedAt,
            cinema.UpdatedAt);
    }
}
