using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Persons;

public sealed class PersonService : IPersonService
{
    private const int MaxNameLength = 200;
    private const int MaxNationalityLength = 100;
    private const int MaxGenderLength = 20;
    private const int MaxPhotoUrlLength = 500;
    private const int MaxPhotoPublicIdLength = 255;

    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    private readonly IPersonRepository _personRepository;

    public PersonService(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public Task<PersonPageResult> GetPersonsPageAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? DefaultPage : page;
        var safePageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        return _personRepository.GetPersonsPageAsync(searchTerm, safePage, safePageSize, cancellationToken);
    }

    public Task<Person?> GetPersonByIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _personRepository.GetByIdAsync(personId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Person? Person)> CreatePersonAsync(
        CreatePersonInput input,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(input.Name);
        if (normalizedName is null)
        {
            return (false, "Name is required", null);
        }

        if (normalizedName.Length > MaxNameLength)
        {
            return (false, $"Name must be at most {MaxNameLength} characters", null);
        }

        var normalized = NormalizeOptionalFields(input.Biography, input.Nationality, input.Gender, input.PhotoUrl, input.PhotoPublicId);
        if (normalized.Error is not null)
        {
            return (false, normalized.Error, null);
        }

        var now = DateTime.UtcNow;
        var person = new Person
        {
            Name = normalizedName,
            Biography = normalized.Biography,
            DateOfBirth = input.DateOfBirth,
            Nationality = normalized.Nationality,
            Gender = normalized.Gender,
            PhotoUrl = normalized.PhotoUrl,
            PhotoPublicId = normalized.PhotoPublicId,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createdPerson = await _personRepository.AddAsync(person, cancellationToken);
        return (true, null, createdPerson);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Person? Person)> UpdatePersonAsync(
        int personId,
        UpdatePersonInput input,
        CancellationToken cancellationToken = default)
    {
        var existingPerson = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (existingPerson is null)
        {
            return (false, "Person not found", null);
        }

        var normalizedName = NormalizeName(input.Name);
        if (normalizedName is null)
        {
            return (false, "Name is required", null);
        }

        if (normalizedName.Length > MaxNameLength)
        {
            return (false, $"Name must be at most {MaxNameLength} characters", null);
        }

        var normalized = NormalizeOptionalFields(input.Biography, input.Nationality, input.Gender, input.PhotoUrl, input.PhotoPublicId);
        if (normalized.Error is not null)
        {
            return (false, normalized.Error, null);
        }

        var updatedPerson = await _personRepository.UpdateAsync(
            personId,
            new PersonUpdateData(
                normalizedName,
                normalized.Biography,
                input.DateOfBirth,
                normalized.Nationality,
                normalized.Gender,
                normalized.PhotoUrl,
                normalized.PhotoPublicId,
                DateTime.UtcNow),
            cancellationToken);

        return updatedPerson is null
            ? (false, "Person not found", null)
            : (true, null, updatedPerson);
    }

    public async Task<DeletePersonResult> DeletePersonAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        var existingPerson = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (existingPerson is null)
        {
            return new DeletePersonResult(DeletePersonStatus.NotFound, "Person not found", Array.Empty<string>());
        }

        var assignedMovieTitles = await _personRepository.GetAssignedMovieTitlesAsync(personId, cancellationToken);
        if (assignedMovieTitles.Count > 0)
        {
            return new DeletePersonResult(
                DeletePersonStatus.AssignedToMovies,
                "Person is assigned to movies.",
                assignedMovieTitles);
        }

        var deleted = await _personRepository.DeleteAsync(personId, cancellationToken);

        return deleted
            ? new DeletePersonResult(DeletePersonStatus.Succeeded, null, Array.Empty<string>())
            : new DeletePersonResult(DeletePersonStatus.NotFound, "Person not found", Array.Empty<string>());
    }

    private static string? NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static (string? Error, string? Biography, string? Nationality, string? Gender, string? PhotoUrl, string? PhotoPublicId)
        NormalizeOptionalFields(string? biography, string? nationality, string? gender, string? photoUrl, string? photoPublicId)
    {
        var normalizedBiography = NormalizeOptional(biography);
        var normalizedNationality = NormalizeOptional(nationality);
        var normalizedGender = NormalizeOptional(gender);
        var normalizedPhotoUrl = NormalizeOptional(photoUrl);
        var normalizedPhotoPublicId = NormalizeOptional(photoPublicId);

        if (normalizedNationality is not null && normalizedNationality.Length > MaxNationalityLength)
        {
            return ($"Nationality must be at most {MaxNationalityLength} characters", null, null, null, null, null);
        }

        if (normalizedGender is not null && normalizedGender.Length > MaxGenderLength)
        {
            return ($"Gender must be at most {MaxGenderLength} characters", null, null, null, null, null);
        }

        if (normalizedPhotoUrl is not null && normalizedPhotoUrl.Length > MaxPhotoUrlLength)
        {
            return ($"PhotoUrl must be at most {MaxPhotoUrlLength} characters", null, null, null, null, null);
        }

        if (normalizedPhotoPublicId is not null && normalizedPhotoPublicId.Length > MaxPhotoPublicIdLength)
        {
            return ($"PhotoPublicId must be at most {MaxPhotoPublicIdLength} characters", null, null, null, null, null);
        }

        return (null, normalizedBiography, normalizedNationality, normalizedGender, normalizedPhotoUrl, normalizedPhotoPublicId);
    }
}
