using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Infrastructure.Persistence;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class MovieRepository : IMovieRepository
{
    private readonly CinemaBookingDbContext _db;

    public MovieRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Movie>> GetMoviesByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _db.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.ShowingFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<Movie?> GetMovieByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.MovieID == id, cancellationToken);
    }

    public async Task<List<Movie>> SearchMoviesAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        return await _db.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Where(m => m.Title.Contains(keyword))
            .ToListAsync(cancellationToken);
    }
}