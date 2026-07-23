using CinemaBooking.API.Contracts.Seats;
using ApiSeatSelector = CinemaBooking.API.Contracts.Seats.SeatSelector;
using CinemaBooking.Application.Seats;
using AppSeatSelector = CinemaBooking.Application.Seats.SeatSelector;
using CinemaBooking.Application.SeatTypes;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CinemaBooking.Application.ActivityLogs;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/rooms/{roomId:int}")]
[Authorize(Roles = Roles.Manager)]
public sealed class SeatController : ControllerBase
{
    private readonly ISeatService _seatService;
    private readonly ISeatTypeService _seatTypeService;
    private readonly IManagerCinemaScopeService _managerCinemaScopeService;
    private readonly IActivityLogService _activityLogs;

    public SeatController(
        ISeatService seatService,
        ISeatTypeService seatTypeService,
        IManagerCinemaScopeService managerCinemaScopeService,
        IActivityLogService activityLogs)
    {
        _seatService = seatService;
        _seatTypeService = seatTypeService;
        _managerCinemaScopeService = managerCinemaScopeService;
        _activityLogs = activityLogs;
    }

    [HttpGet("seats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeats(
        int roomId,
        [FromQuery] int? seatId,
        [FromQuery] string? rows,
        [FromQuery] string? columns,
        CancellationToken cancellationToken)
    {
        if (seatId is <= 0)
            return BadRequest(new { success = false, message = "SeatId must be greater than 0" });

        var rowFilter = SplitValues(rows)
            .Select(value => value.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (rowFilter.Any(value => value.Any(character => !char.IsLetter(character))))
            return BadRequest(new { success = false, message = "Rows must contain comma-separated row labels" });

        var columnValues = SplitValues(columns);
        if (columnValues.Any(value => !int.TryParse(value, out var column) || column <= 0))
            return BadRequest(new { success = false, message = "Columns must contain comma-separated positive numbers" });
        var columnFilter = columnValues.Select(int.Parse).ToHashSet();

        var seats = await _seatService.GetSeatsByRoomAsync(roomId, cancellationToken);
        if (seats is null)
            return NotFound(new { success = false, message = "Room not found" });

        var filteredSeats = seats
            .Where(seat => !seatId.HasValue || seat.SeatID == seatId.Value)
            .Where(seat => rowFilter.Count == 0 || rowFilter.Contains(seat.SeatRow))
            .Where(seat => columnFilter.Count == 0 || columnFilter.Contains(seat.SeatCol))
            .ToList();

        return Ok(ToSeatMapResponse(roomId, filteredSeats));
    }

    [HttpPost("seats/generate")]
    public async Task<IActionResult> GenerateSeats(
        int roomId,
        [FromBody] SeatGenerateRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();

        var result = await _seatService.GenerateSeatsAsync(
            roomId,
            request.Rows,
            request.Columns,
            request.SeatTypeId,
            request.Status,
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        if (!this.TryAuditActorId(out var adminId)) return Unauthorized();
        await _activityLogs.RecordAsync(adminId, AdminActionTypes.GenerateSeat,
            "Room", roomId, $"Generated seats for room {roomId}", this.AuditIpAddress(), cancellationToken);

        return Ok(ToSeatMapResponse(roomId, result.Result!.Seats));
    }

    [HttpPatch("seats/bulk")]
    public async Task<IActionResult> BulkUpdateSeats(
        int roomId,
        [FromBody] SeatBulkRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();

        var result = await _seatService.BulkUpdateAsync(
            roomId,
            ToAppSelectors(request.Selector, request.Selectors),
            request.Update.SeatTypeId,
            request.Update.Status,
            request.Update.IsGap,
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        if (!this.TryAuditActorId(out var adminId)) return Unauthorized();
        await _activityLogs.RecordAsync(adminId, AdminActionTypes.UpdateSeat,
            "Room", roomId, $"Updated seats in room {roomId}", this.AuditIpAddress(), cancellationToken);

        var seats = await _seatService.GetSeatsByRoomAsync(roomId, cancellationToken);
        return Ok(ToSeatMapResponse(roomId, seats!));
    }

    [HttpDelete("seats/bulk")]
    public async Task<IActionResult> BulkDeleteSeats(
        int roomId,
        [FromBody] SeatBulkDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();

        var result = await _seatService.BulkDeleteAsync(
            roomId,
            ToAppSelectors(request.Selector, request.Selectors),
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        if (!this.TryAuditActorId(out var adminId)) return Unauthorized();
        await _activityLogs.RecordAsync(adminId, AdminActionTypes.DeleteSeat,
            "Room", roomId, $"Deleted seats in room {roomId}", this.AuditIpAddress(), cancellationToken);

        var seats = await _seatService.GetSeatsByRoomAsync(roomId, cancellationToken);
        return Ok(ToSeatMapResponse(roomId, seats!));
    }

    private async Task<IActionResult> ToErrorResponseAsync(
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        if (errorMessage == "Seat type not found")
        {
            var seatTypes = await _seatTypeService.GetSeatTypesAsync(cancellationToken);
            return BadRequest(new
            {
                success = false,
                message = errorMessage,
                availableSeatTypes = seatTypes.Select(ToSeatTypeResponse)
            });
        }

        if (errorMessage == CinemaScopeMessages.AccessDenied)
            return CinemaScopeForbidden();

        if (errorMessage is "Room not found" or "Seat not found")
        {
            return NotFound(new { success = false, message = errorMessage });
        }

        if (errorMessage is "Room has active or upcoming schedules"
            or "Seat has related booking or hold records"
            or "One or more seats have related booking or hold records"
            or "Seat already exists in this room position")
        {
            return Conflict(new { success = false, message = errorMessage });
        }

        return BadRequest(new { success = false, message = errorMessage });
    }

    private static SeatResponse ToResponse(Seat seat)
    {
        return new SeatResponse(
            seat.SeatID,
            seat.RoomID,
            seat.SeatRow,
            seat.SeatCol,
            $"{seat.SeatRow}{seat.SeatCol}",
            seat.SeatTypeID,
            seat.SeatType is null ? null : EnumValueMapper.ToApiValue(seat.SeatType.TypeName),
            EnumValueMapper.ToApiValue(seat.Status),
            seat.IsGap);
    }

    private static SeatMapResponse ToSeatMapResponse(
        int roomId,
        IReadOnlyCollection<Seat> seats)
    {
        return new SeatMapResponse(
            roomId,
            seats
                .OrderBy(seat => seat.SeatRow)
                .ThenBy(seat => seat.SeatCol)
                .GroupBy(seat => seat.SeatRow)
                .Select(group => new SeatMapRow(
                    group.Key,
                    group.Select(ToResponse).ToList()))
                .ToList());
    }

    private static AppSeatSelector ToAppSelector(ApiSeatSelector selector)
    {
        if (selector is null)
        {
            return new AppSeatSelector(string.Empty, Array.Empty<string>());
        }

        return new AppSeatSelector(
            selector.Mode,
            selector.Target
                .Select(element => element.ValueKind switch
                {
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.String => element.GetString() ?? string.Empty,
                    _ => element.GetRawText()
                })
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList());
    }

    private static IReadOnlyCollection<AppSeatSelector> ToAppSelectors(
        ApiSeatSelector? selector,
        IReadOnlyCollection<ApiSeatSelector>? selectors)
    {
        var result = new List<AppSeatSelector>();
        if (selector is not null)
            result.Add(ToAppSelector(selector));
        if (selectors is not null)
            result.AddRange(selectors.Where(item => item is not null).Select(ToAppSelector));
        return result;
    }

    private static IReadOnlyCollection<string> SplitValues(string? values) =>
        string.IsNullOrWhiteSpace(values)
            ? Array.Empty<string>()
            : values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static object ToSeatTypeResponse(SeatType seatType)
    {
        return new
        {
            seatTypeId = seatType.SeatTypeID,
            typeName = seatType.TypeName,
            capacity = seatType.Capacity,
            extraPrice = seatType.ExtraPrice
        };
    }

    private async Task<int?> GetManagerCinemaIdAsync(CancellationToken cancellationToken) =>
        int.TryParse(User.FindFirst("userId")?.Value, out var userId)
            ? await _managerCinemaScopeService.GetAssignedCinemaIdAsync(userId, cancellationToken)
            : null;

    private ObjectResult CinemaScopeForbidden() => StatusCode(
        StatusCodes.Status403Forbidden,
        new { success = false, message = CinemaScopeMessages.AccessDenied });
}
