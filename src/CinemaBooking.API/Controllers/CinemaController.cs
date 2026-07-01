using CinemaBooking.API.Contracts.Cinemas;
using CinemaBooking.Application.Cinemas;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/cinemas")]
public sealed class CinemaController : ControllerBase
{
    private readonly ICinemaService _cinemaService;

    public CinemaController(ICinemaService cinemaService)
    {
        _cinemaService = cinemaService;
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
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = ToResponse(result.Cinema!);

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

        return NoContent();
    }

    private static CinemaResponse ToResponse(Cinema cinema)
    {
        return new CinemaResponse(
            cinema.CinemaID,
            cinema.CinemaName,
            cinema.Address,
            EnumValueMapper.ToApiValue(cinema.Status),
            cinema.CreatedAt,
            cinema.UpdatedAt);
    }
}
