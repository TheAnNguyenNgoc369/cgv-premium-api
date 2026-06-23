using CinemaBooking.API.Contracts.Showtimes;
using CinemaBooking.Application.Showtimes;
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

    [HttpGet("movies/{movieId}/showtimes")]
    public async Task<IActionResult> GetShowtimesByMovie(
        int movieId,
        [FromQuery] DateOnly? date,
        [FromQuery] int? cinemaId,
        CancellationToken cancellationToken)
    {
        var showtimes = await _showtimeService.GetShowtimesByMovieAsync(
            movieId, date, cinemaId, cancellationToken);

        var response = showtimes.Select(s => new ShowtimeListResponse(
            s.ShowtimeID,
            s.StartTime,
            s.EndTime,
            s.BasePrice,
            s.RoomID,
            s.Room.RoomName,
            s.Room.RoomType,
            s.Room.CinemaID,
            s.Room.Cinema.CinemaName
        ));

        return Ok(response);
    }

    [HttpGet("showtimes/{id}")]
    public async Task<IActionResult> GetShowtimeById(
        int id,
        CancellationToken cancellationToken)
    {
        var showtime = await _showtimeService.GetShowtimeByIdAsync(id, cancellationToken);

        if (showtime is null)
            return NotFound(new { message = "Không tìm thấy suất chiếu" });

        var response = new ShowtimeDetailResponse(
            showtime.ShowtimeID,
            showtime.StartTime,
            showtime.EndTime,
            showtime.BasePrice,
            showtime.Status,
            showtime.MovieID,
            showtime.Movie.Title,
            showtime.Movie.PosterURL,
            showtime.Movie.DurationMin,
            showtime.Movie.AgeRating,
            showtime.RoomID,
            showtime.Room.RoomName,
            showtime.Room.RoomType,
            showtime.Room.CinemaID,
            showtime.Room.Cinema.CinemaName,
            showtime.Room.Cinema.Address
        );

        return Ok(response);
    }

    [HttpGet("showtimes/{id}/seats")]
    public async Task<IActionResult> GetSeatMap(
        int id,
        CancellationToken cancellationToken)
    {
        var seatMap = await _showtimeService.GetSeatMapAsync(id, cancellationToken);

        if (seatMap is null)
            return NotFound(new { message = "Không tìm thấy suất chiếu" });

        var response = new SeatMapResponse(
            seatMap.ShowtimeID,
            seatMap.RoomName,
            seatMap.RoomType,
            seatMap.Seats.Select(s => new SeatResponse(
                s.SeatID, s.SeatRow, s.SeatCol, s.SeatType, s.ExtraPrice, s.Price, s.Status
            )).ToList()
        );

        return Ok(response);
    }
}