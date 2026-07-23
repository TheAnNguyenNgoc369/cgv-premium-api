using CinemaBooking.API.Contracts.Genres;
using CinemaBooking.Application.Genres;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.Application.ActivityLogs;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/genres")]
public sealed class GenreController : ControllerBase
{
    private readonly IGenreService _genreService;
    private readonly IActivityLogService _activityLogs;

    public GenreController(IGenreService genreService, IActivityLogService activityLogs)
    {
        _genreService = genreService;
        _activityLogs = activityLogs;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetGenres(CancellationToken cancellationToken)
    {
        var genres = await _genreService.GetGenresAsync(cancellationToken);

        return Ok(genres.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGenreById(
        int id,
        CancellationToken cancellationToken)
    {
        var genre = await _genreService.GetGenreByIdAsync(id, cancellationToken);

        if (genre is null)
        {
            return NotFound(new { success = false, message = "Genre not found" });
        }

        return Ok(ToResponse(genre));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateGenre(
        [FromBody] GenreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _genreService.CreateGenreAsync(
            request.GenreName,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        if (!this.TryAuditActorId(out var adminId)) return Unauthorized();
        var response = ToResponse(result.Genre!);
        await _activityLogs.RecordAsync(adminId, AdminActionTypes.CreateGenre,
            "Genre", response.GenreId, $"Created genre {response.GenreId}", this.AuditIpAddress(), cancellationToken);

        return CreatedAtAction(
            nameof(GetGenreById),
            new { id = response.GenreId },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateGenre(
        int id,
        [FromBody] GenreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _genreService.UpdateGenreAsync(
            id,
            request.GenreName,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Genre not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        if (!this.TryAuditActorId(out var adminId)) return Unauthorized();
        await _activityLogs.RecordAsync(adminId, AdminActionTypes.UpdateGenre,
            "Genre", id, $"Updated genre {id}", this.AuditIpAddress(), cancellationToken);
        return Ok(ToResponse(result.Genre!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteGenre(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _genreService.DeleteGenreAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Genre not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return Conflict(new { success = false, message = result.ErrorMessage });
        }

        if (!this.TryAuditActorId(out var adminId)) return Unauthorized();
        await _activityLogs.RecordAsync(adminId, AdminActionTypes.DeleteGenre,
            "Genre", id, $"Deleted genre {id}", this.AuditIpAddress(), cancellationToken);
        return NoContent();
    }

    private static GenreResponse ToResponse(Genre genre)
    {
        return new GenreResponse(
            genre.GenreID,
            genre.GenreName);
    }
}
