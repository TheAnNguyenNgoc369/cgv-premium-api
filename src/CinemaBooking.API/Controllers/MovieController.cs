using CinemaBooking.API.Contracts.Images;
using CinemaBooking.Application.Movie;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieEntity = CinemaBooking.Domain.Entities.Movie;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/movie")]
[Authorize(Roles = Roles.Admin + "," + Roles.Manager + "," + Roles.Staff)]
public sealed class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MovieController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpPut("{id:int}/poster")]
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
