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

    public Task<List<Person>> GetPersonsAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Persons.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmed = searchTerm.Trim();
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{trimmed}%"));
        }

        return query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Person?> GetByIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        int? excludingPersonId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Persons
            .AsNoTracking()
            .Where(p => p.Name == name);

        if (excludingPersonId.HasValue)
        {
            query = query.Where(p => p.PersonId != excludingPersonId.Value);
        }

        return query.AnyAsync(cancellationToken);
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
        string name,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var person = await _dbContext.Persons
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);

        if (person is null)
        {
            return null;
        }

        person.Name = name;
        person.UpdatedAt = updatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return person;
    }

    public Task<bool> IsAssignedToAnyMovieAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MoviePersons
            .AnyAsync(mp => mp.PersonId == personId, cancellationToken);
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
}
