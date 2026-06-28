using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IRoomRepository
{
    Task<List<Room>> GetRoomsAsync(CancellationToken cancellationToken = default);

    Task<Room?> GetByIdAsync(int roomId, CancellationToken cancellationToken = default);

    Task<bool> CinemaExistsAsync(int cinemaId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsInCinemaAsync(
        int cinemaId,
        string roomName,
        int? excludingRoomId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountSeatsAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<Room> AddAsync(Room room, CancellationToken cancellationToken = default);

    Task<Room?> UpdateAsync(
        int roomId,
        int cinemaId,
        string roomName,
        string roomType,
        string status,
        string? description,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveOrUpcomingShowtimesAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int roomId, CancellationToken cancellationToken = default);
}
