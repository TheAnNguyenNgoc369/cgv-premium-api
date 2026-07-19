using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Persons;

public enum DeletePersonStatus
{
    Succeeded,
    NotFound,
    AssignedToMovies
}

public sealed record DeletePersonResult(
    DeletePersonStatus Status,
    string? ErrorMessage,
    IReadOnlyList<string> AssignedMovieTitles);

public interface IPersonService
{
    Task<PersonPageResult> GetPersonsPageAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Person?> GetPersonByIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Person? Person)> CreatePersonAsync(
        CreatePersonInput input,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Person? Person)> UpdatePersonAsync(
        int personId,
        UpdatePersonInput input,
        CancellationToken cancellationToken = default);

    Task<DeletePersonResult> DeletePersonAsync(
        int personId,
        CancellationToken cancellationToken = default);
}
