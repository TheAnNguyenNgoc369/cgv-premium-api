using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Cinemas;

public interface ICinemaService
{
    Task<List<Cinema>> GetCinemasAsync(CancellationToken cancellationToken = default);

    Task<List<Cinema>> GetActiveCinemasAsync(CancellationToken cancellationToken = default);

    Task<Cinema?> GetCinemaByIdAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Cinema? Cinema)> CreateCinemaAsync(
        string cinemaName,
        string address,
        string? status,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Cinema? Cinema)> UpdateCinemaAsync(
        int cinemaId,
        string cinemaName,
        string address,
        string? status,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteCinemaAsync(
        int cinemaId,
        CancellationToken cancellationToken = default);
}
