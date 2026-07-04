using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Application.Common.Security;

namespace CinemaBooking.Application.Seats;

public sealed class SeatService : ISeatService
{
    private const int MaxRows = 100;
    private const int MaxColumns = 100;
    private const int MaxPositions = MaxRows * MaxColumns;
    private readonly ISeatRepository _seatRepository;
    private readonly IUnitOfWork? _unitOfWork;

    public SeatService(ISeatRepository seatRepository, IUnitOfWork? unitOfWork = null)
    {
        _seatRepository = seatRepository;
        _unitOfWork = unitOfWork;
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

    public async Task<Seat?> GetSeatByIdAsync(
        int roomId,
        int seatId,
        CancellationToken cancellationToken = default)
    {
        if (await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken) is null)
        {
            return null;
        }

        return await _seatRepository.GetSeatByIdAsync(roomId, seatId, cancellationToken);
    }

    public async Task<SeatLayoutResult?> GetLayoutAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        if (await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken) is null)
        {
            return null;
        }

        var seats = await _seatRepository.GetSeatsByRoomAsync(roomId, cancellationToken);
        var totalRows = seats.Count == 0
            ? 0
            : seats.Max(seat => ToRowNumber(seat.SeatRow));
        var totalCols = seats.Count == 0
            ? 0
            : seats.Max(seat => seat.SeatCol);

        return new SeatLayoutResult(totalRows, totalCols, seats);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> CreateSeatAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        int? seatTypeId,
        string? status,
        bool isGap,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
            return (false, "Room not found", null);
        if (managerCinemaId.HasValue && room.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied, null);
        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
            return (false, "Room has active or upcoming schedules", null);

        var normalizedRowLabel = NormalizeRowLabel(rowLabel);
        if (normalizedRowLabel is null)
            return (false, "RowLabel is required", null);

        if (seatNumber <= 0)
            return (false, "SeatNumber must be greater than 0", null);

        if (await _seatRepository.SeatPositionExistsAsync(
                roomId,
                normalizedRowLabel,
                seatNumber,
                excludingSeatId: null,
                cancellationToken))
        {
            return (false, "Seat already exists in this room position", null);
        }

        if (isGap)
        {
            var gapSeat = new Seat
            {
                RoomID = roomId,
                SeatRow = normalizedRowLabel,
                SeatCol = seatNumber,
                SeatTypeID = null,
                Status = "inactive",
                IsGap = true
            };

            var createdGap = await ExecuteInTransactionAsync(
                () => _seatRepository.AddAsync(gapSeat, cancellationToken), cancellationToken);
            return createdGap is null
                ? (false, "Seat already exists in this room position", null)
                : (true, null, createdGap);
        }

        var validation = await ValidateSeatAsync(
            roomId,
            rowLabel,
            seatNumber,
            seatTypeId ?? 0,
            status ?? string.Empty,
            excludingSeatId: null,
            cancellationToken);

        if (!validation.Succeeded)
            return (false, validation.ErrorMessage, null);

        var createdSeat = new Seat
        {
            RoomID = roomId,
            SeatRow = validation.RowLabel!,
            SeatCol = seatNumber,
            SeatTypeID = validation.SeatType!.SeatTypeID,
            Status = validation.Status!,
            IsGap = false
        };

        var savedSeat = await ExecuteInTransactionAsync(
            () => _seatRepository.AddAsync(createdSeat, cancellationToken), cancellationToken);
        return savedSeat is null
            ? (false, "Seat already exists in this room position", null)
            : (true, null, savedSeat);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, SeatGenerateResult? Result)> GenerateSeatsAsync(
        int roomId,
        int rows,
        int columns,
        int seatTypeId,
        string status,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
            return (false, "Room not found", null);
        if (managerCinemaId.HasValue && room.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied, null);
        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
            return (false, "Room has active or upcoming schedules", null);
        if (rows <= 0)
            return (false, "Rows must be greater than 0", null);
        if (rows > MaxRows)
            return (false, $"Rows must not exceed {MaxRows}", null);
        if (columns <= 0)
            return (false, "Columns must be greater than 0", null);
        if (columns > MaxColumns)
            return (false, $"Columns must not exceed {MaxColumns}", null);
        if ((long)rows * columns > MaxPositions)
            return (false, $"Total positions must not exceed {MaxPositions}", null);
        if (await _seatRepository.CountSeatsByRoomAsync(roomId, cancellationToken) > 0)
            return (false, "Room already has seats", null);

        var seatType = await _seatRepository.GetSeatTypeByIdAsync(seatTypeId, cancellationToken);
        if (seatType is null)
            return (false, "Seat type not found", null);

        var normalizedStatus = NormalizeSeatStatus(status);
        if (normalizedStatus is null)
            return (false, "Status must be ACTIVE, DISABLED, MAINTENANCE, or INACTIVE", null);

        var seats = new List<Seat>();
        for (var row = 1; row <= rows; row++)
        {
            var rowLabel = ToRowLabel(row);
            for (var col = 1; col <= columns; col++)
            {
                seats.Add(new Seat
                {
                    RoomID = roomId,
                    SeatRow = rowLabel,
                    SeatCol = col,
                    SeatTypeID = seatType.SeatTypeID,
                    Status = normalizedStatus,
                    IsGap = false
                });
            }
        }

        var createdSeats = await ExecuteInTransactionAsync(
            () => _seatRepository.ReplaceLayoutAsync(roomId, seats, cancellationToken), cancellationToken);
        return (true, null, new SeatGenerateResult(rows, columns, createdSeats));
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Seat? Seat)> UpdateSeatAsync(
        int roomId,
        int seatId,
        int? seatTypeId,
        string? status,
        bool? isGap,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
            return (false, "Room not found", null);
        if (managerCinemaId.HasValue && room.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied, null);

        var seat = await _seatRepository.GetSeatByIdAsync(roomId, seatId, cancellationToken);
        if (seat is null)
            return (false, "Seat not found", null);

        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
            return (false, "Room has active or upcoming schedules", null);

        if (seatTypeId is null && status is null && isGap is null)
            return (false, "At least one update field is required", null);

        var normalizedStatus = status is not null
            ? NormalizeSeatStatus(status)
            : seat.Status;

        if (status is not null && normalizedStatus is null)
            return (false, "Status must be ACTIVE, DISABLED, MAINTENANCE, or INACTIVE", null);

        var targetIsGap = isGap ?? seat.IsGap;
        var targetSeatTypeId = seat.SeatTypeID;
        var targetStatus = normalizedStatus;

        if (targetIsGap)
        {
            if (seatTypeId is not null)
                return (false, "Cannot set SeatTypeId for a gap seat", null);

            targetSeatTypeId = null;
            targetStatus = "inactive";
        }
        else
        {
            if (seat.IsGap && !targetIsGap)
            {
                if (seatTypeId is null)
                    return (false, "SeatTypeId is required when converting a gap into a seat.", null);
                if (status is null)
                    return (false, "Status is required when converting a gap into a seat.", null);
            }

            if (seatTypeId is not null)
            {
                if (seatTypeId <= 0)
                    return (false, "SeatTypeId is required.", null);

                var seatType = await _seatRepository.GetSeatTypeByIdAsync(seatTypeId.Value, cancellationToken);
                if (seatType is null)
                    return (false, "Seat type not found", null);

                targetSeatTypeId = seatType.SeatTypeID;
            }

            if (seat.IsGap && targetSeatTypeId is null)
                return (false, "SeatTypeId is required.", null);
        }

        var updatedSeat = await ExecuteInTransactionAsync(
            () => _seatRepository.UpdateAsync(
                roomId, seatId, targetSeatTypeId, targetStatus!, targetIsGap, cancellationToken),
            cancellationToken);

        return updatedSeat is null
            ? (false, "Seat not found", null)
            : (true, null, updatedSeat);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, List<Seat> Seats)> BulkUpdateAsync(
        int roomId,
        SeatSelector selector,
        int? seatTypeId,
        string? status,
        bool? isGap,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
            return (false, "Room not found", []);
        if (managerCinemaId.HasValue && room.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied, []);
        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
            return (false, "Room has active or upcoming schedules", []);
        if (selector is null)
            return (false, "Selector is required", []);
        var selectorError = ValidateSelector(selector);
        if (selectorError is not null)
            return (false, selectorError, []);
        if (seatTypeId is null && status is null && isGap is null)
            return (false, "At least one update field is required", []);

        SeatType? seatType = null;
        if (seatTypeId is not null)
        {
            if (seatTypeId <= 0)
                return (false, "SeatTypeId is required.", []);

            seatType = await _seatRepository.GetSeatTypeByIdAsync(seatTypeId.Value, cancellationToken);
            if (seatType is null)
                return (false, "Seat type not found", []);
        }

        var seats = await _seatRepository.GetSeatsBySelectorAsync(roomId, selector, cancellationToken);
        if (seats.Count == 0)
            return (true, null, []);

        foreach (var seat in seats)
        {
            if (await _seatRepository.HasSeatRelationsAsync(seat.SeatID, cancellationToken))
                return (false, "One or more seats have related booking or hold records", []);
        }

        var normalizedStatus = status is not null
            ? NormalizeSeatStatus(status)
            : null;

        if (status is not null && normalizedStatus is null)
            return (false, "Status must be ACTIVE, DISABLED, MAINTENANCE, or INACTIVE", []);

        var plannedUpdates = new List<(Seat Seat, int? SeatTypeId, string Status, bool IsGap)>();
        foreach (var seat in seats)
        {
            var targetIsGap = isGap ?? seat.IsGap;
            var targetSeatTypeId = seat.SeatTypeID;
            var targetStatus = seat.Status;

            if (targetIsGap)
            {
                if (seatTypeId is not null)
                    return (false, "Cannot set SeatTypeId for a gap seat", []);
                targetSeatTypeId = null;
                targetStatus = "inactive";
            }
            else
            {
                if (seat.IsGap)
                {
                    if (seatTypeId is null)
                        return (false, "SeatTypeId is required when converting a gap into a seat.", []);
                    if (status is null)
                        return (false, "Status is required when converting a gap into a seat.", []);
                }

                if (seatTypeId is not null)
                    targetSeatTypeId = seatType!.SeatTypeID;
                if (normalizedStatus is not null)
                    targetStatus = normalizedStatus;
                if (targetSeatTypeId is null)
                    return (false, "SeatTypeId is required.", []);
            }

            plannedUpdates.Add((seat, targetSeatTypeId, targetStatus, targetIsGap));
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            var updatedSeats = new List<Seat>();
            foreach (var update in plannedUpdates)
            {
                var updatedSeat = await _seatRepository.UpdateAsync(
                    roomId, update.Seat.SeatID, update.SeatTypeId,
                    update.Status, update.IsGap, cancellationToken);

                if (updatedSeat is null)
                    throw new InvalidOperationException("Seat disappeared during bulk update.");

                updatedSeats.Add(updatedSeat);
            }

            return (true, (string?)null, updatedSeats);
        }, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> BulkDeleteAsync(
        int roomId,
        SeatSelector selector,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
            return (false, "Room not found");
        if (managerCinemaId.HasValue && room.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied);
        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
            return (false, "Room has active or upcoming schedules");
        if (selector is null)
            return (false, "Selector is required");
        var selectorError = ValidateSelector(selector);
        if (selectorError is not null)
            return (false, selectorError);

        var seats = await _seatRepository.GetSeatsBySelectorAsync(roomId, selector, cancellationToken);
        if (seats.Count == 0)
            return (true, null);

        foreach (var seat in seats)
        {
            if (await _seatRepository.HasSeatRelationsAsync(seat.SeatID, cancellationToken))
                return (false, "One or more seats have related booking or hold records");
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            foreach (var seat in seats)
            {
                var updatedSeat = await _seatRepository.UpdateAsync(
                    roomId, seat.SeatID, seat.SeatTypeID, "inactive", seat.IsGap, cancellationToken);

                if (updatedSeat is null)
                    throw new InvalidOperationException("Seat disappeared during bulk delete.");
            }

            return (true, (string?)null);
        }, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteSeatAsync(
        int roomId,
        int seatId,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
            return (false, "Room not found");
        if (managerCinemaId.HasValue && room.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied);
        var seat = await _seatRepository.GetSeatByIdAsync(roomId, seatId, cancellationToken);
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

        var deleted = await ExecuteInTransactionAsync(
            () => _seatRepository.DeleteAsync(seatId, cancellationToken), cancellationToken);
        return deleted
            ? (true, null)
            : (false, "Seat not found");
    }

    public async Task<(bool Succeeded, string? ErrorMessage, List<Seat> Seats)> ReplaceLayoutAsync(
        int roomId,
        int totalRows,
        int totalCols,
        IReadOnlyCollection<SeatLayoutSeatItem> seats,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await _seatRepository.GetRoomByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            return (false, "Room not found", []);
        }
        if (managerCinemaId.HasValue && room.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied, []);

        if (await _seatRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
        {
            return (false, "Room has active or upcoming schedules", []);
        }

        if (totalRows <= 0)
        {
            return (false, "TotalRows must be greater than 0", []);
        }
        if (totalRows > MaxRows)
            return (false, $"TotalRows must not exceed {MaxRows}", []);

        if (totalCols <= 0)
        {
            return (false, "TotalCols must be greater than 0", []);
        }
        if (totalCols > MaxColumns)
            return (false, $"TotalCols must not exceed {MaxColumns}", []);
        if ((long)totalRows * totalCols > MaxPositions || seats.Count > MaxPositions)
            return (false, $"Total positions must not exceed {MaxPositions}", []);

        if (seats is null)
        {
            return (false, "Seats are required", []);
        }

        var validation = await BuildLayoutSeatsAsync(
            roomId,
            totalRows,
            totalCols,
            seats,
            cancellationToken);
        if (!validation.Succeeded)
        {
            return (false, validation.ErrorMessage, []);
        }

        var updatedSeats = await ExecuteInTransactionAsync(
            () => _seatRepository.ReplaceLayoutAsync(roomId, validation.Seats, cancellationToken),
            cancellationToken);

        return (true, null, updatedSeats);
    }

    private static string? ValidateSelector(SeatSelector selector)
    {
        var mode = selector.Mode?.Trim().ToUpperInvariant();
        if (mode is not ("IDS" or "ROWS" or "COLS"))
            return "Selector mode must be IDS, ROWS, or COLS";
        return selector.Target is null || selector.Target.Count == 0
            ? "Selector target must contain at least one value"
            : null;
    }

    private Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation, CancellationToken cancellationToken) =>
        _unitOfWork is null
            ? operation()
            : _unitOfWork.ExecuteInTransactionAsync(operation, cancellationToken);

    private async Task<(bool Succeeded, string? ErrorMessage, Room? Room, SeatType? SeatType, string? RowLabel, string? Status)> ValidateSeatAsync(
        int roomId,
        string rowLabel,
        int seatNumber,
        int seatTypeId,
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

        if (seatTypeId <= 0)
        {
            return (false, "SeatTypeId is required.", null, null, null, null);
        }

        var seatType = await _seatRepository.GetSeatTypeByIdAsync(seatTypeId, cancellationToken);
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

    private async Task<(bool Succeeded, string? ErrorMessage, List<Seat> Seats)> BuildLayoutSeatsAsync(
        int roomId,
        int totalRows,
        int totalCols,
        IReadOnlyCollection<SeatLayoutSeatItem> items,
        CancellationToken cancellationToken)
    {
        var layoutSeats = new List<Seat>();
        var positions = new HashSet<(string RowLabel, int ColIndex)>();
        var seatTypes = new Dictionary<int, SeatType>();

        foreach (var item in items)
        {
            if (item.IsGap)
            {
                if (item.RowLabel is null)
                {
                    return (false, "RowLabel is required", []);
                }

                var gapRowLabel = NormalizeRowLabel(item.RowLabel);
                if (gapRowLabel is null)
                {
                    return (false, "RowLabel is required", []);
                }

                var gapRowIndex = ToRowNumber(gapRowLabel);
                if (gapRowIndex <= 0 || gapRowIndex > totalRows)
                {
                    return (false, "RowLabel must be within TotalRows", []);
                }

                if (item.ColIndex <= 0 || item.ColIndex > totalCols)
                {
                    return (false, "ColIndex must be within TotalCols", []);
                }

                if (!positions.Add((gapRowLabel, item.ColIndex)))
                {
                    return (false, "Seat position is duplicated in the layout", []);
                }

                layoutSeats.Add(new Seat
                {
                    RoomID = roomId,
                    SeatRow = gapRowLabel,
                    SeatCol = item.ColIndex,
                    SeatTypeID = null,
                    Status = "inactive",
                    IsGap = true
                });

                continue;
            }

            var rowLabel = NormalizeRowLabel(item.RowLabel ?? string.Empty);
            if (rowLabel is null)
            {
                return (false, "RowLabel is required", []);
            }

            var rowIndex = ToRowNumber(rowLabel);
            if (rowIndex <= 0 || rowIndex > totalRows)
            {
                return (false, "RowLabel must be within TotalRows", []);
            }

            if (item.ColIndex <= 0 || item.ColIndex > totalCols)
            {
                return (false, "ColIndex must be within TotalCols", []);
            }

            if (!string.IsNullOrWhiteSpace(item.SeatName)
                && !string.Equals(
                    item.SeatName.Trim(),
                    GetSeatCode(rowLabel, item.ColIndex),
                    StringComparison.OrdinalIgnoreCase))
            {
                return (false, "SeatName must match RowLabel + ColIndex", []);
            }

            if (item.SeatTypeId is null or <= 0)
            {
                return (false, "SeatTypeId is required.", []);
            }

            if (!seatTypes.TryGetValue(item.SeatTypeId.Value, out var seatType))
            {
                seatType = await _seatRepository.GetSeatTypeByIdAsync(
                    item.SeatTypeId.Value,
                    cancellationToken);
                if (seatType is null)
                {
                    return (false, "Seat type not found", []);
                }

                seatTypes[item.SeatTypeId.Value] = seatType;
            }

            var status = NormalizeSeatStatus(item.Status);
            if (status is null)
            {
                return (false, "Status must be ACTIVE, DISABLED, MAINTENANCE, or INACTIVE", []);
            }

            if (!positions.Add((rowLabel, item.ColIndex)))
            {
                return (false, "Seat position is duplicated in the layout", []);
            }

            layoutSeats.Add(new Seat
            {
                RoomID = roomId,
                SeatRow = rowLabel,
                SeatCol = item.ColIndex,
                SeatTypeID = seatType.SeatTypeID,
                Status = status
            });
        }

        return (true, null, layoutSeats);
    }

    private static string? NormalizeSeatStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? null
            : EnumValueMapper.Validate(status, "Status", DatabaseEnumMappings.SeatStatuses).DatabaseValue;
    }

    private static string? NormalizeRowLabel(string? rowLabel)
    {
        return string.IsNullOrWhiteSpace(rowLabel)
            ? null
            : rowLabel.Trim().ToUpperInvariant();
    }

    private static string ToRowLabel(int rowIndex)
    {
        var label = string.Empty;
        while (rowIndex > 0)
        {
            rowIndex--;
            label = (char)('A' + rowIndex % 26) + label;
            rowIndex /= 26;
        }

        return label;
    }

    private static string GetSeatCode(string rowLabel, int seatNumber)
    {
        return $"{rowLabel}{seatNumber}";
    }

    private static int ToRowNumber(string rowLabel)
    {
        var rowNumber = 0;
        foreach (var character in rowLabel)
        {
            if (character is < 'A' or > 'Z')
            {
                return 0;
            }

            rowNumber = rowNumber * 26 + character - 'A' + 1;
        }

        return rowNumber;
    }

}
