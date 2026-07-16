using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IPersonRepository
{
    Task<List<Person>> GetPersonsAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<Person?> GetByIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int? excludingPersonId = null,
        CancellationToken cancellationToken = default);

    Task<Person> AddAsync(
        Person person,
        CancellationToken cancellationToken = default);

    Task<Person?> UpdateAsync(
        int personId,
        string name,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToAnyMovieAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int personId,
        CancellationToken cancellationToken = default);
}
