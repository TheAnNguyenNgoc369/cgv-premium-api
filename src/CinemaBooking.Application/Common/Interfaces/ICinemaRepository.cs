using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface ICinemaRepository
{
    Task<List<Cinema>> GetCinemasAsync(CancellationToken cancellationToken = default);

    Task<List<Cinema>> GetActiveCinemasAsync(CancellationToken cancellationToken = default);

    Task<Cinema?> GetByIdAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<Cinema> AddAsync(Cinema cinema, CancellationToken cancellationToken = default);

    Task<Cinema?> UpdateAsync(
        int cinemaId,
        string cinemaName,
        string address,
        decimal? latitude,
        decimal? longitude,
        string status,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);

    Task<bool> HasRoomsAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<bool> HasAssignedUsersAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveAssignedStaffAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<bool> HasAssignedStaffAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<bool> HasUpcomingShowtimesAsync(
        int cinemaId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<Cinema?> SoftDeleteAsync(
        int cinemaId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);
}
