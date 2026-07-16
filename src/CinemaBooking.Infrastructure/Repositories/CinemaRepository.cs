using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class CinemaRepository : ICinemaRepository
{
    private readonly CinemaBookingDbContext _db;

    public CinemaRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Cinema>> GetCinemasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Cinemas
            .AsNoTracking()
            .OrderBy(c => c.CinemaID)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Cinema>> GetActiveCinemasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Cinemas
            .AsNoTracking()
            .Where(c => c.Status == "active")
            .OrderBy(c => c.CinemaID)
            .ToListAsync(cancellationToken);
    }

    public async Task<Cinema?> GetByIdAsync(
        int cinemaId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Cinemas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CinemaID == cinemaId, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string cinemaName,
        int? excludingCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = cinemaName.ToUpper();
        var query = _db.Cinemas.AsNoTracking()
            .Where(c => c.CinemaName.ToUpper() == normalizedName);

        if (excludingCinemaId.HasValue)
            query = query.Where(c => c.CinemaID != excludingCinemaId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public async Task<Cinema> AddAsync(
        Cinema cinema,
        CancellationToken cancellationToken = default)
    {
        _db.Cinemas.Add(cinema);
        await _db.SaveChangesAsync(cancellationToken);

        return cinema;
    }

    public async Task<Cinema?> UpdateAsync(
        int cinemaId,
        string cinemaName,
        string address,
        decimal? latitude,
        decimal? longitude,
        string status,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var cinema = await _db.Cinemas
            .FirstOrDefaultAsync(c => c.CinemaID == cinemaId, cancellationToken);

        if (cinema is null)
        {
            return null;
        }

        cinema.CinemaName = cinemaName;
        cinema.Address = address;
        cinema.Latitude = latitude;
        cinema.Longitude = longitude;
        cinema.Status = status;
        cinema.UpdatedAt = updatedAt;

        await _db.SaveChangesAsync(cancellationToken);

        return cinema;
    }

    public async Task<bool> HasRoomsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Rooms
            .AnyAsync(r => r.CinemaID == cinemaId, cancellationToken);
    }

    public async Task<bool> HasAssignedUsersAsync(
        int cinemaId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AnyAsync(u => u.CinemaID == cinemaId, cancellationToken);
    }

    public Task<bool> HasActiveAssignedStaffAsync(
        int cinemaId,
        CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking()
            .AnyAsync(u => u.CinemaID == cinemaId
                && u.Status == UserStatuses.Active
                && (u.Role == Roles.Manager || u.Role == Roles.Staff),
                cancellationToken);

    public Task<bool> HasAssignedStaffAsync(
        int cinemaId,
        CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking()
            .AnyAsync(u => u.CinemaID == cinemaId
                && (u.Role == Roles.Manager || u.Role == Roles.Staff),
                cancellationToken);

    public Task<bool> HasUpcomingShowtimesAsync(
        int cinemaId,
        DateTime now,
        CancellationToken cancellationToken = default) =>
        _db.Showtimes.AsNoTracking()
            .AnyAsync(s => s.Room.CinemaID == cinemaId
                && s.Status == "scheduled"
                && s.StartTime > now,
                cancellationToken);

    public async Task<Cinema?> SoftDeleteAsync(
        int cinemaId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var cinema = await _db.Cinemas
            .FirstOrDefaultAsync(c => c.CinemaID == cinemaId, cancellationToken);

        if (cinema is null)
        {
            return null;
        }

        cinema.Status = "inactive";
        cinema.UpdatedAt = updatedAt;

        await _db.SaveChangesAsync(cancellationToken);

        return cinema;
    }
}
