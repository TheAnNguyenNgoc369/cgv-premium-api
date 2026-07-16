using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Persons;

public sealed class PersonService : IPersonService
{
    private const int MaxNameLength = 200;

    private readonly IPersonRepository _personRepository;

    public PersonService(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public Task<List<Person>> GetPersonsAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        return _personRepository.GetPersonsAsync(searchTerm, cancellationToken);
    }

    public Task<Person?> GetPersonByIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _personRepository.GetByIdAsync(personId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Person? Person)> CreatePersonAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName is null)
        {
            return (false, "Name is required", null);
        }

        if (normalizedName.Length > MaxNameLength)
        {
            return (false, $"Name must be at most {MaxNameLength} characters", null);
        }

        if (await _personRepository.NameExistsAsync(normalizedName, cancellationToken: cancellationToken))
        {
            return (false, "Name must be unique", null);
        }

        var now = DateTime.UtcNow;
        var person = new Person
        {
            Name = normalizedName,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createdPerson = await _personRepository.AddAsync(person, cancellationToken);
        return (true, null, createdPerson);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Person? Person)> UpdatePersonAsync(
        int personId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var existingPerson = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (existingPerson is null)
        {
            return (false, "Person not found", null);
        }

        var normalizedName = NormalizeName(name);
        if (normalizedName is null)
        {
            return (false, "Name is required", null);
        }

        if (normalizedName.Length > MaxNameLength)
        {
            return (false, $"Name must be at most {MaxNameLength} characters", null);
        }

        if (await _personRepository.NameExistsAsync(normalizedName, personId, cancellationToken))
        {
            return (false, "Name must be unique", null);
        }

        var updatedPerson = await _personRepository.UpdateAsync(
            personId,
            normalizedName,
            DateTime.UtcNow,
            cancellationToken);

        return updatedPerson is null
            ? (false, "Person not found", null)
            : (true, null, updatedPerson);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeletePersonAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        var existingPerson = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (existingPerson is null)
        {
            return (false, "Person not found");
        }

        if (await _personRepository.IsAssignedToAnyMovieAsync(personId, cancellationToken))
        {
            return (false, "Person is assigned to a movie");
        }

        var deleted = await _personRepository.DeleteAsync(personId, cancellationToken);

        return deleted
            ? (true, null)
            : (false, "Person not found");
    }

    private static string? NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim();
    }
}
