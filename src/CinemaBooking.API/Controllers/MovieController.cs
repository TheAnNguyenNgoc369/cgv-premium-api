using CinemaBooking.API.Contracts.Movies;
using CinemaBooking.Application.Movie;
using CinemaBooking.Application.Common.Enums;
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
        [FromQuery] string? status = null,
        [FromQuery] string? genreId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGenreIds(genreId, out var genreIds))
        {
            return BadRequest(new
            {
                success = false,
                message = "GenreId must be a comma-separated list of positive integers."
            });
        }

        var movies = await _movieService.GetMoviesAsync(status, genreIds, cancellationToken);

        var response = movies.Select(ToListResponse);

        return Ok(response);
    }

    private static bool TryParseGenreIds(string? value, out IReadOnlyCollection<int> genreIds)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            genreIds = Array.Empty<int>();
            return true;
        }

        var parsedIds = new HashSet<int>();
        foreach (var part in value.Split(',', StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var id) || id <= 0)
            {
                genreIds = Array.Empty<int>();
                return false;
            }

            parsedIds.Add(id);
        }

        genreIds = parsedIds;
        return true;
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
            return NotFound(new { success = false, message = "Movie not found." });
        }

        return Ok(ToDetailResponse(movie));
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchMovies(
        [FromQuery] string keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { success = false, message = "Please enter a search keyword." });
        }

        var movies = await _movieService.SearchMoviesAsync(keyword, cancellationToken);

        var response = movies.Select(ToListResponse);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> CreateMovie(
        [FromBody] MovieRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.CreateMovieAsync(
            request.Title,
            request.Genres,
            request.AgeRating,
            request.Director,
            request.Cast,
            request.Synopsis,
            request.DurationMinutes,
            request.ShowingFromDate,
            request.ShowingToDate,
            request.PosterUrl,
            request.PosterPublicId,
            request.TrailerUrl,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = ToDetailResponse(result.Movie!);

        return CreatedAtAction(
            nameof(GetMovieById),
            new { id = response.MovieId },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> UpdateMovie(
        int id,
        [FromBody] MovieRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.UpdateMovieAsync(
            id,
            request.Title,
            request.Genres,
            request.AgeRating,
            request.Director,
            request.Cast,
            request.Synopsis,
            request.DurationMinutes,
            request.ShowingFromDate,
            request.ShowingToDate,
            request.PosterUrl,
            request.PosterPublicId,
            request.TrailerUrl,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Movie not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(ToDetailResponse(result.Movie!));
    }

    [HttpPut("{id:int}/poster")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> UpdatePoster(
        int id,
        [FromForm] CinemaBooking.API.Contracts.Images.ImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
            return BadRequest(new { success = false, message = "Poster file is required" });

        await using var stream = request.File.OpenReadStream();
        var result = await _movieService.UpdatePosterAsync(
            id,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            cancellationToken);

        if (!result.Succeeded)
            return result.ErrorMessage == "Movie not found"
                ? NotFound(new { success = false, message = result.ErrorMessage })
                : BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(ToDetailResponse(result.Movie!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> DeleteMovie(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.DeleteMovieAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Movie not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return Conflict(new { success = false, message = result.ErrorMessage });
        }

        return NoContent();
    }

    private static MovieListResponse ToListResponse(MovieEntity movie)
    {
        return new MovieListResponse(
            movie.MovieID,
            movie.Title,
            movie.MovieGenres.Select(mg => mg.Genre.GenreName).ToList(),
            EnumValueMapper.ToApiValue(movie.AgeRating),
            movie.PosterURL,
            movie.DurationMin,
            EnumValueMapper.ToApiValue(movie.Status));
    }

    private static MovieDetailResponse ToDetailResponse(MovieEntity movie)
    {
        return new MovieDetailResponse(
            movie.MovieID,
            movie.Title,
            movie.MovieGenres.Select(mg => mg.Genre.GenreName).ToList(),
            EnumValueMapper.ToApiValue(movie.AgeRating),
            movie.Director,
            movie.Cast,
            movie.Description,
            movie.DurationMin,
            movie.ShowingFrom,
            movie.ShowingTo,
            movie.PosterURL,
            movie.PosterPublicId,
            movie.TrailerURL,
            EnumValueMapper.ToApiValue(movie.Status));
    }
}
