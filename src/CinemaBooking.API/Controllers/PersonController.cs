using CinemaBooking.API.Contracts.Persons;
using CinemaBooking.Application.Persons;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/persons")]
public sealed class PersonController : ControllerBase
{
    private readonly IPersonService _personService;

    public PersonController(IPersonService personService)
    {
        _personService = personService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedPersonAutocompleteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersons(
        [FromQuery] string? search = null,
        [FromQuery] int page = PersonService.DefaultPage,
        [FromQuery] int pageSize = PersonService.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _personService.GetPersonsPageAsync(search, page, pageSize, cancellationToken);

        var effectivePage = page < 1 ? PersonService.DefaultPage : page;
        var effectivePageSize = pageSize < 1
            ? PersonService.DefaultPageSize
            : Math.Min(pageSize, PersonService.MaxPageSize);

        return Ok(new PagedPersonAutocompleteResponse(
            result.Items.Select(ToAutocompleteItem).ToList(),
            effectivePage,
            effectivePageSize,
            result.Total));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPersonById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var person = await _personService.GetPersonByIdAsync(id, cancellationToken);
        if (person is null)
        {
            return NotFound(new { success = false, message = "Person not found" });
        }

        return Ok(ToResponse(person));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePerson(
        [FromBody] CreatePersonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _personService.CreatePersonAsync(
            new CreatePersonInput(
                request.Name,
                request.Biography,
                request.DateOfBirth,
                request.Nationality,
                request.Gender,
                request.PhotoUrl,
                request.PhotoPublicId),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = ToResponse(result.Person!);
        return CreatedAtAction(
            nameof(GetPersonById),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePerson(
        int id,
        [FromBody] UpdatePersonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _personService.UpdatePersonAsync(
            id,
            new UpdatePersonInput(
                request.Name,
                request.Biography,
                request.DateOfBirth,
                request.Nationality,
                request.Gender,
                request.PhotoUrl,
                request.PhotoPublicId),
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Person not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(ToResponse(result.Person!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePerson(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _personService.DeletePersonAsync(id, cancellationToken);

        return result.Status switch
        {
            DeletePersonStatus.Succeeded => NoContent(),
            DeletePersonStatus.NotFound => NotFound(new { success = false, message = result.ErrorMessage }),
            DeletePersonStatus.AssignedToMovies => Conflict(new
            {
                message = result.ErrorMessage,
                movies = result.AssignedMovieTitles
            }),
            _ => Conflict(new { success = false, message = result.ErrorMessage })
        };
    }

    private static PersonAutocompleteItem ToAutocompleteItem(Person person) =>
        new(person.PersonId, person.Name, person.PhotoUrl, person.Nationality);

    private static PersonResponse ToResponse(Person person) =>
        new(
            person.PersonId,
            person.Name,
            person.Biography,
            person.DateOfBirth,
            person.Nationality,
            person.Gender,
            person.PhotoUrl,
            person.PhotoPublicId,
            person.CreatedAt,
            person.UpdatedAt);
}
