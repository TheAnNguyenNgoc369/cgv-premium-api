using CinemaBooking.API.Contracts.Movies;
using CinemaBooking.Application.Movies;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MovieController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    // GET /api/movies?status=now_showing
    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string status = "now_showing",
        CancellationToken cancellationToken = default)
    {
        var movies = await _movieService.GetMoviesByStatusAsync(status, cancellationToken);

        var response = movies.Select(m => new MovieListResponse(
            m.MovieID,
            m.Title,
            m.AgeRating,
            m.PosterURL,
            m.DurationMin,
            m.Status,
            m.MovieGenres.Select(mg => mg.Genre.GenreName).ToList()
        ));

        return Ok(response);
    }

    // GET /api/movies/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMovieById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var movie = await _movieService.GetMovieByIdAsync(id, cancellationToken);

        if (movie is null)
            return NotFound(new { message = "Không tìm thấy phim" });

        var response = new MovieDetailResponse(
            movie.MovieID,
            movie.Title,
            movie.AgeRating,
            movie.Director,
            movie.Cast,
            movie.Description,
            movie.PosterURL,
            movie.TrailerURL,
            movie.DurationMin,
            movie.ShowingFrom,
            movie.ShowingTo,
            movie.Status,
            movie.MovieGenres.Select(mg => mg.Genre.GenreName).ToList()
        );

        return Ok(response);
    }

    // GET /api/movies/search?keyword=avengers
    [HttpGet("search")]
    public async Task<IActionResult> SearchMovies(
        [FromQuery] string keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Vui lòng nhập từ khoá tìm kiếm" });

        var movies = await _movieService.SearchMoviesAsync(keyword, cancellationToken);

        var response = movies.Select(m => new MovieListResponse(
            m.MovieID,
            m.Title,
            m.AgeRating,
            m.PosterURL,
            m.DurationMin,
            m.Status,
            m.MovieGenres.Select(mg => mg.Genre.GenreName).ToList()
        ));

        return Ok(response);
    }
}