using CinemaBooking.API.Contracts.Images;
using CinemaBooking.API.Contracts.Movies;
using CinemaBooking.Application.Movie;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/movie")]
public sealed class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MovieController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    [AllowAnonymous]
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
            m.MovieGenres.Select(mg => mg.Genre.GenreName).ToList()));

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMovieById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var movie = await _movieService.GetMovieByIdAsync(id, cancellationToken);

        if (movie is null)
        {
            return NotFound(new { message = "Không tìm thấy phim" });
        }

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
            movie.MovieGenres.Select(mg => mg.Genre.GenreName).ToList());

        return Ok(response);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchMovies(
        [FromQuery] string keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { message = "Vui lòng nhập từ khoá tìm kiếm" });
        }

        var movies = await _movieService.SearchMoviesAsync(keyword, cancellationToken);

        var response = movies.Select(m => new MovieListResponse(
            m.MovieID,
            m.Title,
            m.AgeRating,
            m.PosterURL,
            m.DurationMin,
            m.Status,
            m.MovieGenres.Select(mg => mg.Genre.GenreName).ToList()));

        return Ok(response);
    }

    [HttpPut("{id:int}/poster")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager + "," + Roles.Staff)]
    public async Task<IActionResult> UploadPoster(
        int id,
        [FromForm] ImageUploadRequest model,
        CancellationToken cancellationToken)
    {
        if (model.File is null)
        {
            return BadRequest(new { message = "Image file is required" });
        }

        await using var stream = model.File.OpenReadStream();
        var result = await _movieService.UploadPosterAsync(
            id,
            stream,
            model.File.FileName,
            model.File.ContentType,
            model.File.Length,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Movie not found")
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            secureUrl = result.Movie!.PosterURL,
            publicId = result.Movie.PosterPublicId,
            movie = ToPosterResponse(result.Movie)
        });
    }

    [HttpDelete("{id:int}/poster")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager + "," + Roles.Staff)]
    public async Task<IActionResult> DeletePoster(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.DeletePosterAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Movie not found")
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToPosterResponse(result.Movie!));
    }

    private static object ToPosterResponse(MovieEntity movie)
    {
        return new
        {
            movie.MovieID,
            movie.Title,
            movie.PosterURL,
            movie.PosterPublicId,
            movie.UpdatedAt
        };
    }
}
