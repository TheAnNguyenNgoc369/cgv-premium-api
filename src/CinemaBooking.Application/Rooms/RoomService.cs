using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;

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

    public Task<Room?> GetRoomByIdAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        return _roomRepository.GetByIdAsync(roomId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Room? Room)> CreateRoomAsync(
        int cinemaId,
        string name,
        string type,
        int capacity,
        string status,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateRoomAsync(
            cinemaId,
            name,
            type,
            capacity,
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
            RoomType = validation.RoomType!,
            Capacity = capacity,
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
        string type,
        int capacity,
        string status,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var existingRoom = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        if (existingRoom is null)
        {
            return (false, "Room not found", null);
        }

        var validation = await ValidateRoomAsync(
            cinemaId,
            name,
            type,
            capacity,
            status,
            roomId,
            cancellationToken);

        if (!validation.Succeeded)
        {
            return (false, validation.ErrorMessage, null);
        }

        var updatedRoom = await _roomRepository.UpdateAsync(
            roomId,
            cinemaId,
            name.Trim(),
            validation.RoomType!,
            capacity,
            validation.Status!,
            NormalizeNullable(description),
            cancellationToken);

        return updatedRoom is null
            ? (false, "Room not found", null)
            : (true, null, updatedRoom);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var existingRoom = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        if (existingRoom is null)
        {
            return (false, "Room not found");
        }

        if (await _roomRepository.HasActiveOrUpcomingShowtimesAsync(roomId, cancellationToken))
        {
            return (false, "Room has active or upcoming schedules");
        }

        var deleted = await _roomRepository.DeleteAsync(roomId, cancellationToken);

        return deleted
            ? (true, null)
            : (false, "Room not found");
    }

    private async Task<(bool Succeeded, string? ErrorMessage, string? RoomType, string? Status)> ValidateRoomAsync(
        int cinemaId,
        string name,
        string type,
        int capacity,
        string status,
        int? excludingRoomId,
        CancellationToken cancellationToken)
    {
        if (cinemaId <= 0)
        {
            return (false, "CinemaId is required", null, null);
        }

        if (!await _roomRepository.CinemaExistsAsync(cinemaId, cancellationToken))
        {
            return (false, "Cinema not found", null, null);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Name is required", null, null);
        }

        if (capacity <= 0)
        {
            return (false, "Capacity must be greater than 0", null, null);
        }

        var normalizedType = NormalizeRoomType(type);
        if (normalizedType is null)
        {
            return (false, "Type must be STANDARD, VIP, IMAX, or THREE_D", null, null);
        }

        var normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus is null)
        {
            return (false, "Status must be ACTIVE, MAINTENANCE, or INACTIVE", null, null);
        }

        if (await _roomRepository.NameExistsInCinemaAsync(
                cinemaId,
                name.Trim(),
                excludingRoomId,
                cancellationToken))
        {
            return (false, "Room name must be unique within the cinema", null, null);
        }

        return (true, null, normalizedType, normalizedStatus);
    }

    private static string? NormalizeRoomType(string type)
    {
        return string.IsNullOrWhiteSpace(type)
            ? null
            : EnumValueMapper.Validate(type, "Type", DatabaseEnumMappings.RoomTypes).DatabaseValue;
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
