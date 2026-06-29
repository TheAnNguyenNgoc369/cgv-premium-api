using CinemaBooking.API.Contracts.Showtimes;
using CinemaBooking.Application.Showtimes;
using CinemaBooking.API.Contracts.Cinemas;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api")]
public sealed class ShowtimeController : ControllerBase
{
    private readonly IShowtimeService _showtimeService;

    public ShowtimeController(IShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
    }

    [HttpGet("showtimes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetShowtimes(
        [FromQuery] int? movieId, [FromQuery] int? cinemaId,
        [FromQuery] string? movieName, [FromQuery] string? roomName,
        [FromQuery] DateOnly? date, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "startTime", [FromQuery] string? sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var result = await _showtimeService.GetShowtimesAsync(
            movieId, cinemaId, movieName, roomName, date, status,
            page, pageSize, sortBy, sortDir, cancellationToken);
        if (!result.Succeeded) return BadRequest(new { success = false, message = result.ErrorMessage });
        var data = result.Page!;
        var items = new List<ShowtimeListItemResponse>();
        foreach (var showtime in data.Items)
            items.Add(await ToListResponseAsync(showtime, cancellationToken));
        return Ok(new PagedShowtimeResponse(items, data.Page, data.PageSize, data.TotalItems,
            (int)Math.Ceiling(data.TotalItems / (double)data.PageSize)));
    }

    [HttpPost("showtimes")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> CreateShowtime(
        ShowtimeRequest request, CancellationToken cancellationToken)
    {
        var result = await _showtimeService.CreateShowtimeAsync(
            request.MovieId, request.RoomId, request.StartTime, request.BasePrice, request.Status, cancellationToken);
        if (!result.Succeeded) return MapWriteError(result.ErrorMessage);
        var response = await ToManagementResponseAsync(result.Showtime!, cancellationToken);
        return CreatedAtAction(nameof(GetShowtimeById), new { id = response.ShowtimeId }, response);
    }

    [HttpPut("showtimes/{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> UpdateShowtime(
        int id, ShowtimeRequest request, CancellationToken cancellationToken)
    {
        var result = await _showtimeService.UpdateShowtimeAsync(
            id, request.MovieId, request.RoomId, request.StartTime, request.BasePrice, request.Status, cancellationToken);
        if (!result.Succeeded) return MapWriteError(result.ErrorMessage);
        return Ok(await ToManagementResponseAsync(result.Showtime!, cancellationToken));
    }

    [HttpDelete("showtimes/{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> DeleteShowtime(int id, CancellationToken cancellationToken)
    {
        var result = await _showtimeService.DeleteShowtimeAsync(id, cancellationToken);
        if (!result.Succeeded) return MapWriteError(result.ErrorMessage);
        return NoContent();
    }

    [HttpGet("showtimes/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetShowtimeById(
        int id,
        CancellationToken cancellationToken)
    {
        var showtime = await _showtimeService.GetShowtimeByIdAsync(id, cancellationToken);

        if (showtime is null)
            return NotFound(new { success = false, message = "Showtime not found." });

        return Ok(await ToManagementResponseAsync(showtime, cancellationToken));
    }

    private async Task<ShowtimeManagementResponse> ToManagementResponseAsync(
        Showtime showtime, CancellationToken cancellationToken) =>
        new(showtime.ShowtimeID,
            new(showtime.MovieID, showtime.Movie.Title, showtime.Movie.AgeRating,
                showtime.Movie.DurationMin, showtime.Movie.PosterURL),
            new(showtime.RoomID, showtime.Room.RoomName, showtime.Room.RoomType, showtime.Room.Capacity),
            showtime.StartTime, showtime.EndTime, showtime.BasePrice,
            EnumValueMapper.ToApiValue(showtime.Status),
            await _showtimeService.IsSoldOutAsync(showtime, cancellationToken));

    private async Task<ShowtimeListItemResponse> ToListResponseAsync(
        Showtime showtime, CancellationToken cancellationToken) =>
        new(showtime.ShowtimeID,
            new(showtime.MovieID, showtime.Movie.Title, showtime.Movie.AgeRating,
                showtime.Movie.DurationMin, showtime.Movie.PosterURL),
            new(showtime.RoomID, showtime.Room.RoomName, showtime.Room.RoomType, showtime.Room.Capacity),
            new CinemaSummaryResponse(
                showtime.Room.CinemaID,
                showtime.Room.Cinema.CinemaName,
                showtime.Room.Cinema.Address,
                EnumValueMapper.ToApiValue(showtime.Room.Cinema.Status)),
            showtime.StartTime, showtime.EndTime, showtime.BasePrice,
            EnumValueMapper.ToApiValue(showtime.Status),
            await _showtimeService.IsSoldOutAsync(showtime, cancellationToken));

    private IActionResult MapWriteError(string? message) => message switch
    {
        "Showtime not found" or "Movie not found" or "Room not found" =>
            NotFound(new { success = false, message }),
        "Showtime conflicts with another showtime in the same room"
            or "Showtime has active bookings or seat holds" =>
            Conflict(new { success = false, message }),
        _ => BadRequest(new { success = false, message })
    };

}
