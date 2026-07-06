using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Cinemas;

public sealed class CinemaService : ICinemaService
{
    private readonly ICinemaRepository _cinemaRepository;

    public CinemaService(ICinemaRepository cinemaRepository)
    {
        _cinemaRepository = cinemaRepository;
    }

    public async Task<List<Cinema>> GetCinemasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _cinemaRepository.GetCinemasAsync(cancellationToken);
    }

    public async Task<List<Cinema>> GetActiveCinemasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _cinemaRepository.GetActiveCinemasAsync(cancellationToken);
    }

    public async Task<Cinema?> GetCinemaByIdAsync(
        int cinemaId,
        CancellationToken cancellationToken = default)
    {
        return await _cinemaRepository.GetByIdAsync(cinemaId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Cinema? Cinema)> CreateCinemaAsync(
        string cinemaName,
        string address,
        double? latitude,
        double? longitude,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateNameAndAddress(cinemaName, address);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var normalizedStatus = NormalizeStatus(status, defaultToActive: true);
        if (normalizedStatus is null)
        {
            return (false, "Status must be active, inactive, or maintenance", null);
        }

        var now = DateTime.UtcNow;
        var cinema = new Cinema
        {
            CinemaName = cinemaName.Trim(),
            Address = address.Trim(),
            Latitude = latitude.HasValue ? Convert.ToDecimal(latitude.Value) : null,
            Longitude = longitude.HasValue ? Convert.ToDecimal(longitude.Value) : null,
            Status = normalizedStatus,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createdCinema = await _cinemaRepository.AddAsync(cinema, cancellationToken);

        return (true, null, createdCinema);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Cinema? Cinema)> UpdateCinemaAsync(
        int cinemaId,
        string cinemaName,
        string address,
        double? latitude,
        double? longitude,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var existingCinema = await _cinemaRepository.GetByIdAsync(cinemaId, cancellationToken);
        if (existingCinema is null)
        {
            return (false, "Cinema not found", null);
        }

        var validationError = ValidateNameAndAddress(cinemaName, address);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var normalizedStatus = NormalizeStatus(status, defaultToActive: false);
        if (normalizedStatus is null)
        {
            return (false, "Status must be active, inactive, or maintenance", null);
        }

        var updatedCinema = await _cinemaRepository.UpdateAsync(
            cinemaId,
            cinemaName.Trim(),
            address.Trim(),
            latitude.HasValue ? Convert.ToDecimal(latitude.Value) : null,
            longitude.HasValue ? Convert.ToDecimal(longitude.Value) : null,
            normalizedStatus,
            DateTime.UtcNow,
            cancellationToken);

        return updatedCinema is null
            ? (false, "Cinema not found", null)
            : (true, null, updatedCinema);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteCinemaAsync(
        int cinemaId,
        CancellationToken cancellationToken = default)
    {
        var existingCinema = await _cinemaRepository.GetByIdAsync(cinemaId, cancellationToken);
        if (existingCinema is null)
        {
            return (false, "Cinema not found");
        }

        if (await _cinemaRepository.HasRoomsAsync(cinemaId, cancellationToken))
        {
            return (false, "Cinema has rooms");
        }

        if (await _cinemaRepository.HasAssignedUsersAsync(cinemaId, cancellationToken))
        {
            return (false, "Cinema has assigned users");
        }

        var deletedCinema = await _cinemaRepository.SoftDeleteAsync(
            cinemaId,
            DateTime.UtcNow,
            cancellationToken);

        return deletedCinema is null
            ? (false, "Cinema not found")
            : (true, null);
    }

    private static string? ValidateNameAndAddress(string cinemaName, string address)
    {
        if (string.IsNullOrWhiteSpace(cinemaName))
        {
            return "CinemaName is required";
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            return "Address is required";
        }

        return null;
    }

    private static string? NormalizeStatus(string? status, bool defaultToActive)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return defaultToActive ? "active" : null;
        }

        return EnumValueMapper.Validate(
            status, "Status", DatabaseEnumMappings.CinemaStatuses).DatabaseValue;
    }
}
