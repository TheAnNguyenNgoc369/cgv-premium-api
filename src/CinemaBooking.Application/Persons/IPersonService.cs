using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Persons;

public interface IPersonService
{
    Task<List<Person>> GetPersonsAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<Person?> GetPersonByIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Person? Person)> CreatePersonAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Person? Person)> UpdatePersonAsync(
        int personId,
        string name,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeletePersonAsync(
        int personId,
        CancellationToken cancellationToken = default);
}
