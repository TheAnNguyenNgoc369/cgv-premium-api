using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IPersonRepository
{
    Task<PersonPageResult> GetPersonsPageAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Person?> GetByIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<Person> AddAsync(
        Person person,
        CancellationToken cancellationToken = default);

    Task<Person?> UpdateAsync(
        int personId,
        PersonUpdateData data,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToAnyMovieAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetAssignedMovieTitlesAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int personId,
        CancellationToken cancellationToken = default);
}

public sealed record PersonPageResult(
    IReadOnlyList<Person> Items,
    int Total);

public sealed record PersonUpdateData(
    string Name,
    string? Biography,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? Gender,
    string? PhotoUrl,
    string? PhotoPublicId,
    DateTime UpdatedAt);
