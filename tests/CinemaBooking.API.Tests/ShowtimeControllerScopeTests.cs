using System.Security.Claims;
using CinemaBooking.API.Controllers;
using CinemaBooking.Application.ActivityLogs;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Application.Showtimes;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class ShowtimeControllerScopeTests
{
    [Fact]
    public async Task GetShowtimes_StaffWithoutCinemaQuery_UsesAssignedCinema()
    {
        var service = new StubShowtimeService();
        var controller = CreateController(service, staffCinemaId: 5);

        var result = await controller.GetShowtimes(
            movieId: null,
            cinemaId: null,
            movieName: null,
            roomName: null,
            date: null,
            status: null,
            cancellationToken: default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(5, service.CapturedCinemaId);
        Assert.Null(service.CapturedManagerCinemaId);
    }

    [Fact]
    public async Task GetShowtimes_StaffRequestsAnotherCinema_ReturnsForbidden()
    {
        var service = new StubShowtimeService();
        var controller = CreateController(service, staffCinemaId: 5);

        var result = await controller.GetShowtimes(
            movieId: null,
            cinemaId: 6,
            movieName: null,
            roomName: null,
            date: null,
            status: null,
            cancellationToken: default);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0, service.GetShowtimesCallCount);
    }

    private static ShowtimeController CreateController(
        StubShowtimeService service,
        int staffCinemaId)
    {
        return new ShowtimeController(
            service,
            new StubCinemaScopeService(staffCinemaId),
            new StubActivityLogService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("userId", "10"),
                        new Claim(ClaimTypes.Role, Roles.Staff)
                    ], "test"))
                }
            }
        };
    }

    private sealed class StubCinemaScopeService(int staffCinemaId) : IManagerCinemaScopeService
    {
        public Task<int?> GetAssignedCinemaIdAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<int?>(null);

        public Task<int?> GetAssignedCinemaIdAsync(
            int userId,
            string role,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<int?>(role == Roles.Staff ? staffCinemaId : null);
    }

    private sealed class StubShowtimeService : IShowtimeService
    {
        public int GetShowtimesCallCount { get; private set; }
        public int? CapturedCinemaId { get; private set; }
        public int? CapturedManagerCinemaId { get; private set; }

        public Task<(bool Succeeded, string? ErrorMessage, ShowtimePageResult? Page)> GetShowtimesAsync(
            int? movieId,
            int? cinemaId,
            string? movieName,
            string? roomName,
            DateOnly? date,
            string? status,
            int page,
            int pageSize,
            string? sortBy,
            string? sortDir,
            int? managerCinemaId = null,
            CancellationToken cancellationToken = default)
        {
            GetShowtimesCallCount++;
            CapturedCinemaId = cinemaId;
            CapturedManagerCinemaId = managerCinemaId;
            return Task.FromResult<(bool, string?, ShowtimePageResult?)>(
                (true, null, new ShowtimePageResult([], new HashSet<int>(), page, pageSize, 0)));
        }

        public Task<(bool Succeeded, string? ErrorMessage, Showtime? Showtime)> CreateShowtimeAsync(
            int movieId,
            int roomId,
            DateTime startTime,
            decimal basePrice,
            int? managerCinemaId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Succeeded, string? ErrorMessage, Showtime? Showtime)> UpdateShowtimeAsync(
            int id,
            int movieId,
            int roomId,
            DateTime startTime,
            decimal basePrice,
            string? status,
            int? managerCinemaId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Succeeded, string? ErrorMessage)> DeleteShowtimeAsync(
            int id,
            int? managerCinemaId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> IsSoldOutAsync(
            Showtime showtime,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Succeeded, string? ErrorMessage, IReadOnlyList<Showtime> Items, IReadOnlySet<int> SoldOutShowtimeIds)> GetShowtimesByRangeAsync(
            DateOnly startDate,
            DateOnly endDate,
            int? cinemaId = null,
            int? managerCinemaId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Showtime?> GetShowtimeByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Showtime?> GetManagedShowtimeByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(SeatMapResult? SeatMap, string? ErrorMessage)> GetSeatMapAsync(
            int showtimeId,
            int? viewerCinemaId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public string GetDisplayStatus(Showtime showtime, DateTime now) =>
            throw new NotSupportedException();

        public bool IsActive(Showtime showtime, DateTime now) =>
            throw new NotSupportedException();
    }

    private sealed class StubActivityLogService : IActivityLogService
    {
        public Task<ActivityLogPage> GetAsync(
            string? actionType,
            string? module,
            int? actorId,
            int? targetUserId,
            string? targetTable,
            int? targetId,
            DateOnly? startDate,
            DateOnly? endDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ActivityLogDetail?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IReadOnlyList<string> GetActionTypes() =>
            throw new NotSupportedException();

        public Task RecordAsync(
            int actorId,
            string actionType,
            string targetTable,
            int targetId,
            string description,
            string ipAddress,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
