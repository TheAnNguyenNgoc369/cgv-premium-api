using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Application.Common.Security;

namespace CinemaBooking.Application.Rooms;

public sealed class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public Task<List<Room>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        return _roomRepository.GetRoomsAsync(cancellationToken);
    }

    public Task<List<Room>> GetRoomsByCinemaIdAsync(
    int cinemaId,
    CancellationToken cancellationToken = default)
{
    return _roomRepository.GetRoomsByCinemaIdAsync(cinemaId, cancellationToken);
}

    public Task<Room?> GetRoomByIdAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return _roomRepository.GetByIdAsync(roomId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Room? Room)> CreateRoomAsync(
        int cinemaId,
        string name,
        int roomTypeId,
        string status,
        string? description,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        if (managerCinemaId.HasValue && cinemaId != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied, null);
        var validation = await ValidateRoomAsync(
            cinemaId,
            name,
            roomTypeId,
            status,
            excludingRoomId: null,
            cancellationToken);

        if (!validation.Succeeded)
        {
            return (false, validation.ErrorMessage, null);
        }

        var room = new Room
        {
            CinemaID = cinemaId,
            RoomName = name.Trim(),
            RoomTypeID = roomTypeId,
            Capacity = 0,
            Status = validation.Status!,
            Description = NormalizeNullable(description),
            CreatedAt = DateTime.UtcNow
        };

        var createdRoom = await _roomRepository.AddAsync(room, cancellationToken);

        return (true, null, createdRoom);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Room? Room)> UpdateRoomAsync(
        int roomId,
        int cinemaId,
        string name,
        int roomTypeId,
        string status,
        string? description,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var existingRoom = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        if (existingRoom is null)
        {
            return (false, "Room not found", null);
        }
        if (managerCinemaId.HasValue
            && (existingRoom.CinemaID != managerCinemaId || cinemaId != managerCinemaId))
            return (false, CinemaScopeMessages.AccessDenied, null);

        var validation = await ValidateRoomAsync(
            cinemaId,
            name,
            roomTypeId,
            status,
            roomId,
            cancellationToken);

        if (!validation.Succeeded)
        {
            return (false, validation.ErrorMessage, null);
        }

        if (existingRoom.Status != "inactive"
            && validation.Status == "inactive"
            && await _roomRepository.HasUpcomingShowtimesAsync(roomId, DateTime.UtcNow, cancellationToken))
        {
            return (false, "Room has upcoming showtimes", null);
        }

        var updatedRoom = await _roomRepository.UpdateAsync(
            roomId,
            cinemaId,
            name.Trim(),
            roomTypeId,
            validation.Status!,
            NormalizeNullable(description),
            cancellationToken);

        return updatedRoom is null
            ? (false, "Room not found", null)
            : (true, null, updatedRoom);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteRoomAsync(
        int roomId,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default)
    {
        var existingRoom = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        if (existingRoom is null)
        {
            return (false, "Room not found");
        }
        if (managerCinemaId.HasValue && existingRoom.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied);

        if (await _roomRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
        {
            return (false, "Room has active or upcoming schedules");
        }

        if (await _roomRepository.HasAnyShowtimesAsync(roomId, cancellationToken))
        {
            return (false, "Room has showtime history");
        }

        var deleted = await _roomRepository.DeleteAsync(roomId, cancellationToken);

        return deleted
            ? (true, null)
            : (false, "Room not found");
    }

    private async Task<(bool Succeeded, string? ErrorMessage, string? Status)> ValidateRoomAsync(
        int cinemaId,
        string name,
        int roomTypeId,
        string status,
        int? excludingRoomId,
        CancellationToken cancellationToken)
    {
        if (cinemaId <= 0)
        {
            return (false, "CinemaId is required", null);
        }

        if (!await _roomRepository.CinemaExistsAsync(cinemaId, cancellationToken))
        {
            return (false, "Cinema not found", null);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Name is required", null);
        }

        if (!await _roomRepository.RoomTypeExistsAsync(roomTypeId, cancellationToken))
        {
            return (false, "Room type not found", null);
        }

        var normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus is null)
        {
            return (false, "Status must be ACTIVE, MAINTENANCE, or INACTIVE", null);
        }

        if (await _roomRepository.NameExistsInCinemaAsync(
                cinemaId,
                name.Trim(),
                excludingRoomId,
                cancellationToken))
        {
            return (false, "Room name must be unique within the cinema", null);
        }

        return (true, null, normalizedStatus);
    }

    private static string? NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? null
            : EnumValueMapper.Validate(status, "Status", DatabaseEnumMappings.RoomStatuses).DatabaseValue;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
