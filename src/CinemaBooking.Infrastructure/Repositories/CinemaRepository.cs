using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class CinemaRepository : ICinemaRepository
{
    private readonly CinemaBookingDbContext _db;

    public CinemaRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Cinema>> GetActiveCinemasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Cinemas
            .Where(c => c.Status == "active")
            .ToListAsync(cancellationToken);
    }
}