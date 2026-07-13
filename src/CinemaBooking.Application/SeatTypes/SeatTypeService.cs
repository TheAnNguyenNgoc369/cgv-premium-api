using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.SeatTypes;

public sealed class SeatTypeService : ISeatTypeService
{
    private readonly ISeatTypeRepository _seatTypeRepository;

    public SeatTypeService(ISeatTypeRepository seatTypeRepository)
    {
        _seatTypeRepository = seatTypeRepository;
    }

    public Task<List<SeatType>> GetSeatTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return _seatTypeRepository.GetAllAsync(cancellationToken);
    }

    public Task<SeatType?> GetSeatTypeByIdAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default)
    {
        return _seatTypeRepository.GetByIdAsync(seatTypeId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, SeatType? SeatType)> CreateSeatTypeAsync(
        string typeName,
        int capacity,
        decimal extraPrice,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(typeName);
        var validationError = Validate(normalizedName, capacity, extraPrice);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        if (await _seatTypeRepository.NameExistsAsync(normalizedName!, cancellationToken: cancellationToken))
        {
            return (false, "Seat type name must be unique.", null);
        }

        var seatType = new SeatType
        {
            TypeName = normalizedName!,
            Capacity = capacity,
            ExtraPrice = extraPrice
        };

        return (true, null, await _seatTypeRepository.AddAsync(seatType, cancellationToken));
    }

    public async Task<(bool Succeeded, string? ErrorMessage, SeatType? SeatType)> UpdateSeatTypeAsync(
        int seatTypeId,
        string typeName,
        int capacity,
        decimal extraPrice,
        CancellationToken cancellationToken = default)
    {
        if (await _seatTypeRepository.GetByIdAsync(seatTypeId, cancellationToken) is null)
        {
            return (false, "Seat type not found.", null);
        }

        var normalizedName = NormalizeName(typeName);
        var validationError = Validate(normalizedName, capacity, extraPrice);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        if (await _seatTypeRepository.NameExistsAsync(
                normalizedName!,
                seatTypeId,
                cancellationToken))
        {
            return (false, "Seat type name must be unique.", null);
        }

        var updatedSeatType = await _seatTypeRepository.UpdateAsync(
            seatTypeId,
            normalizedName!,
            capacity,
            extraPrice,
            cancellationToken);

        return updatedSeatType is null
            ? (false, "Seat type not found.", null)
            : (true, null, updatedSeatType);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteSeatTypeAsync(
        int seatTypeId,
        CancellationToken cancellationToken = default)
    {
        if (await _seatTypeRepository.GetByIdAsync(seatTypeId, cancellationToken) is null)
        {
            return (false, "Seat type not found.");
        }

        if (await _seatTypeRepository.IsUsedByAnySeatAsync(seatTypeId, cancellationToken))
        {
            return (false, "Seat type is currently used by one or more seats.");
        }

        return await _seatTypeRepository.DeleteAsync(seatTypeId, cancellationToken)
            ? (true, null)
            : (false, "Seat type not found.");
    }

    private static string? NormalizeName(string typeName)
    {
        return string.IsNullOrWhiteSpace(typeName)
            ? null
            : typeName.Trim();
    }

    private static string? Validate(string? typeName, int capacity, decimal extraPrice)
    {
        if (typeName is null)
        {
            return "Seat type name is required.";
        }

        if (typeName.Length > 20)
        {
            return "Seat type name must not exceed 20 characters.";
        }

        if (capacity <= 0)
        {
            return "Capacity must be greater than 0.";
        }

        return extraPrice < 0
            ? "Extra price must be greater than or equal to 0."
            : null;
    }
}
