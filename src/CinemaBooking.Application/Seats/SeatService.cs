using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Seats;

public sealed class SeatService : ISeatService
{
    private readonly ISeatRepository _seatRepository;

    public SeatService(ISeatRepository seatRepository)
    {
        _seatRepository = seatRepository;
    }

    public async Task<List<Seat>?> GetSeatsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        if (await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken) is null)
        {
            return null;
        }

        return await _seatRepository.GetSeatsByRoomAsync(roomId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> CreateSeatAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        string? seatCode,
        string type,
        string status,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateSeatAsync(
            roomId,
            rowLabel,
            seatNumber,
            seatCode,
            type,
            status,
            excludingSeatId: null,
            cancellationToken);

        if (!validation.Succeeded)
        {
            return (false, validation.ErrorMessage, null);
        }

        if (await _seatRepository.CountSeatsByRoomAsync(roomId, cancellationToken) >= validation.Room!.Capacity)
        {
            return (false, GetCapacityExceededMessage(validation.Room.Capacity), null);
        }

        var seat = new Seat
        {
            RoomID = roomId,
            SeatRow = validation.RowLabel!,
            SeatCol = seatNumber,
            SeatTypeID = validation.SeatType!.SeatTypeID,
            Status = validation.Status!
        };

        return (true, null, await _seatRepository.AddAsync(seat, cancellationToken));
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> UpdateSeatAsync(
        int roomId,
        int seatId,
        string type,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken) is null)
        {
            return (false, "Room not found", null);
        }

        if (await _seatRepository.GetSeatByIdAsync(roomId, seatId, cancellationToken) is null)
        {
            return (false, "Seat not found", null);
        }

        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
        {
            return (false, "Room has active or upcoming schedules", null);
        }

        var normalizedType = NormalizeSeatType(type);
        if (normalizedType is null)
        {
            return (false, "Type is required.", null);
        }

        var seatType = await _seatRepository.GetSeatTypeByNameAsync(normalizedType, cancellationToken);
        if (seatType is null)
        {
            return (false, "Seat type not found", null);
        }

        var normalizedStatus = NormalizeSeatStatus(status);
        if (normalizedStatus is null)
        {
            return (false, "Status must be ACTIVE, DISABLED, MAINTENANCE, or INACTIVE", null);
        }

        var seat = await _seatRepository.UpdateAsync(
            roomId,
            seatId,
            seatType.SeatTypeID,
            normalizedStatus,
            cancellationToken);

        return seat is null
            ? (false, "Seat not found", null)
            : (true, null, seat);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteSeatAsync(
        int seatId,
        CancellationToken cancellationToken = default)
    {
        var seat = await _seatRepository.GetSeatByIdAsync(seatId, cancellationToken);
        if (seat is null)
        {
            return (false, "Seat not found");
        }

        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(seat.RoomID, cancellationToken))
        {
            return (false, "Room has active or upcoming schedules");
        }

        if (await _seatRepository.HasSeatRelationsAsync(seatId, cancellationToken))
        {
            return (false, "Seat has related booking or hold records");
        }

        return await _seatRepository.DeleteAsync(seatId, cancellationToken)
            ? (true, null)
            : (false, "Seat not found");
    }

    public async Task<(bool Succeeded, string? ErrorMessage, List<Seat> Seats)> ReplaceLayoutAsync(
        int roomId,
        int totalRows,
        int seatsPerRow,
        string seatType,
        string seatStatus,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            return (false, "Room not found", []);
        }

        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
        {
            return (false, "Room has active or upcoming schedules", []);
        }

        if (totalRows <= 0)
        {
            return (false, "TotalRows must be greater than 0", []);
        }

        if (seatsPerRow <= 0)
        {
            return (false, "SeatsPerRow must be greater than 0", []);
        }

        if (totalRows * seatsPerRow > room.Capacity)
        {
            return (false, GetCapacityExceededMessage(room.Capacity), []);
        }

        var normalizedType = NormalizeSeatType(seatType);
        if (normalizedType is null)
        {
            return (false, "SeatType is required.", []);
        }

        var type = await _seatRepository.GetSeatTypeByNameAsync(normalizedType, cancellationToken);
        if (type is null)
        {
            return (false, "Seat type not found", []);
        }

        var normalizedStatus = NormalizeSeatStatus(seatStatus);
        if (normalizedStatus is null)
        {
            return (false, "SeatStatus must be ACTIVE, DISABLED, MAINTENANCE, or INACTIVE", []);
        }

        var seats = new List<Seat>(totalRows * seatsPerRow);
        for (var row = 1; row <= totalRows; row++)
        {
            var rowLabel = ToRowLabel(row);
            for (var seatNumber = 1; seatNumber <= seatsPerRow; seatNumber++)
            {
                seats.Add(new Seat
                {
                    RoomID = roomId,
                    SeatRow = rowLabel,
                    SeatCol = seatNumber,
                    SeatTypeID = type.SeatTypeID,
                    Status = normalizedStatus
                });
            }
        }

        var updatedSeats = await _seatRepository.ReplaceLayoutAsync(roomId, seats, cancellationToken);

        return (true, null, updatedSeats);
    }

    private async Task<(bool Succeeded, string? ErrorMessage, Room? Room, SeatType? SeatType, string? RowLabel, string? Status)> ValidateSeatAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        string? seatCode,
        string type,
        string status,
        int? excludingSeatId,
        CancellationToken cancellationToken)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            return (false, "Room not found", null, null, null, null);
        }

        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
        {
            return (false, "Room has active or upcoming schedules", null, null, null, null);
        }

        var normalizedRowLabel = NormalizeRowLabel(rowLabel);
        if (normalizedRowLabel is null)
        {
            return (false, "RowLabel is required", null, null, null, null);
        }

        if (seatNumber <= 0)
        {
            return (false, "SeatNumber must be greater than 0", null, null, null, null);
        }

        if (!string.IsNullOrWhiteSpace(seatCode)
            && !string.Equals(seatCode.Trim(), GetSeatCode(normalizedRowLabel, seatNumber), StringComparison.OrdinalIgnoreCase))
        {
            return (false, "SeatCode must match RowLabel + SeatNumber", null, null, null, null);
        }

        var normalizedType = NormalizeSeatType(type);
        if (normalizedType is null)
        {
            return (false, "Type is required.", null, null, null, null);
        }

        var seatType = await _seatRepository.GetSeatTypeByNameAsync(normalizedType, cancellationToken);
        if (seatType is null)
        {
            return (false, "Seat type not found", null, null, null, null);
        }

        var normalizedStatus = NormalizeSeatStatus(status);
        if (normalizedStatus is null)
        {
            return (false, "Status must be ACTIVE, DISABLED, MAINTENANCE, or INACTIVE", null, null, null, null);
        }

        if (await _seatRepository.SeatPositionExistsAsync(
                roomId,
                normalizedRowLabel,
                seatNumber,
                excludingSeatId,
                cancellationToken))
        {
            return (false, "Seat already exists in this room position", null, null, null, null);
        }

        return (true, null, room, seatType, normalizedRowLabel, normalizedStatus);
    }

    private static string? NormalizeSeatType(string type)
    {
        return string.IsNullOrWhiteSpace(type)
            ? null
            : type.Trim().ToLowerInvariant();
    }

    private static string? NormalizeSeatStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? null
            : EnumValueMapper.Validate(status, "Status", DatabaseEnumMappings.SeatStatuses).DatabaseValue;
    }

    private static string? NormalizeRowLabel(string rowLabel)
    {
        return string.IsNullOrWhiteSpace(rowLabel)
            ? null
            : rowLabel.Trim().ToUpperInvariant();
    }

    private static string GetSeatCode(string rowLabel, int seatNumber)
    {
        return $"{rowLabel}{seatNumber}";
    }

    private static string GetCapacityExceededMessage(int capacity)
    {
        return $"Room capacity exceeded. Room capacity is {capacity} seats.";
    }

    private static string ToRowLabel(int rowNumber)
    {
        var label = string.Empty;
        while (rowNumber > 0)
        {
            rowNumber--;
            label = (char)('A' + rowNumber % 26) + label;
            rowNumber /= 26;
        }

        return label;
    }
}
