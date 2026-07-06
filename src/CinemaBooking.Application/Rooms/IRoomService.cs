using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Rooms;

public interface IRoomService
{
    Task<List<Room>> GetRoomsAsync(CancellationToken cancellationToken = default);

    Task<List<Room>> GetRoomsByCinemaIdAsync(
    int cinemaId,
    CancellationToken cancellationToken = default);

    Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Room? Room)> CreateRoomAsync(
        int cinemaId,
        string name,
        int roomTypeId,
        string status,
        string? description,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Room? Room)> UpdateRoomAsync(
        int roomId,
        int cinemaId,
        string name,
        int roomTypeId,
        string status,
        string? description,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteRoomAsync(
        int roomId,
        int? managerCinemaId = null,
        CancellationToken cancellationToken = default);
}
