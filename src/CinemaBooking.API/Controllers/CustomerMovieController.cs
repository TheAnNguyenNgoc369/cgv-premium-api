using CinemaBooking.API.Contracts.Showtimes;
using CinemaBooking.Application.Showtimes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("customer/movies")]
public sealed class CustomerMovieController : ControllerBase
{
    private readonly IShowtimeService _showtimeService;

    public CustomerMovieController(IShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
    }

    [HttpGet("{movieId:int}/showtimes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerShowtimeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShowtimes(
        int movieId,
        [FromQuery] int? cinemaId,
        CancellationToken cancellationToken)
    {
        var result = await _showtimeService.GetCustomerShowtimesAsync(
            movieId, cinemaId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage is "Movie not found" or "Cinema not found")
                return NotFound(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = result.Showtimes.Select(showtime => new CustomerShowtimeResponse(
            showtime.ShowtimeID,
            showtime.MovieID,
            showtime.Movie.Title,
            showtime.Room.CinemaID,
            showtime.Room.Cinema.CinemaName,
            showtime.RoomID,
            showtime.Room.RoomName,
            showtime.Room.RoomType,
            showtime.StartTime,
            showtime.EndTime,
            showtime.BasePrice));

        return Ok(response);
    }
}
