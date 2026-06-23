using CinemaBooking.API.Contracts.Cinemas;
using CinemaBooking.Application.Cinemas;
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
    public async Task<IActionResult> GetCinemas(CancellationToken cancellationToken)
    {
        var cinemas = await _cinemaService.GetActiveCinemasAsync(cancellationToken);

        var response = cinemas.Select(c => new CinemaResponse(
            c.CinemaID,
            c.CinemaName,
            c.Address
        ));

        return Ok(response);
    }
}