using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class PersonRepository : IPersonRepository
{
    private readonly CinemaBookingDbContext _dbContext;

    public PersonRepository(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PersonPageResult> GetPersonsPageAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Persons.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Name, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PersonPageResult(items, total);
    }

    public Task<Person?> GetByIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);
    }

    public async Task<Person> AddAsync(
        Person person,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Persons.Add(person);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return person;
    }

    public async Task<Person?> UpdateAsync(
        int personId,
        PersonUpdateData data,
        CancellationToken cancellationToken = default)
    {
        var person = await _dbContext.Persons
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);

        if (person is null)
        {
            return null;
        }

        person.Name = data.Name;
        person.Biography = data.Biography;
        person.DateOfBirth = data.DateOfBirth;
        person.Nationality = data.Nationality;
        person.Gender = data.Gender;
        person.PhotoUrl = data.PhotoUrl;
        person.PhotoPublicId = data.PhotoPublicId;
        person.UpdatedAt = data.UpdatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return person;
    }

    public Task<List<string>> GetAssignedMovieTitlesAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MoviePersons
            .AsNoTracking()
            .Where(mp => mp.PersonId == personId)
            .Select(mp => mp.Movie.Title)
            .Distinct()
            .OrderBy(title => title)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        var person = await _dbContext.Persons
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);

        if (person is null)
        {
            return false;
        }

        _dbContext.Persons.Remove(person);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public Task<PersonHeader?> GetHeaderByIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .Select(p => new PersonHeader(p.PersonId, p.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PersonFilmographyPageResult> GetFilmographyPageAsync(
        int personId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var movieQuery = _dbContext.MoviePersons
            .AsNoTracking()
            .Where(mp => mp.PersonId == personId && mp.Movie.Status != "ended")
            .Select(mp => mp.Movie)
            .Distinct();

        var total = await movieQuery.CountAsync(cancellationToken);

        if (total == 0)
        {
            return new PersonFilmographyPageResult(Array.Empty<PersonFilmographyItem>(), 0);
        }

        var pagedMovieIds = await movieQuery
            .OrderByDescending(m => m.ShowingFrom)
            .ThenByDescending(m => m.MovieID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => m.MovieID)
            .ToListAsync(cancellationToken);

        if (pagedMovieIds.Count == 0)
        {
            return new PersonFilmographyPageResult(Array.Empty<PersonFilmographyItem>(), total);
        }

        var rows = await _dbContext.MoviePersons
            .AsNoTracking()
            .Where(mp => mp.PersonId == personId && pagedMovieIds.Contains(mp.MovieId))
            .Select(mp => new
            {
                mp.MovieId,
                mp.Role,
                mp.DisplayOrder,
                Title = mp.Movie.Title,
                PosterUrl = mp.Movie.PosterURL,
                ReleaseDate = mp.Movie.ShowingFrom,
                Duration = mp.Movie.DurationMin,
                AgeRating = mp.Movie.AgeRating
            })
            .ToListAsync(cancellationToken);

        var orderIndex = pagedMovieIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        var items = rows
            .GroupBy(r => r.MovieId)
            .Select(g =>
            {
                var first = g.First();
                var roles = g
                    .OrderBy(r => r.DisplayOrder)
                    .Select(r => r.Role)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new PersonFilmographyItem(
                    first.MovieId,
                    first.Title,
                    first.PosterUrl,
                    first.ReleaseDate,
                    first.Duration,
                    first.AgeRating,
                    roles);
            })
            .OrderBy(item => orderIndex[item.MovieId])
            .ToList();

        return new PersonFilmographyPageResult(items, total);
    }
}
