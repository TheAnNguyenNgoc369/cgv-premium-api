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

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/rooms/{roomId:int}")]
[Authorize(Roles = Roles.Manager)]
public sealed class SeatController : ControllerBase
{
    private readonly ISeatService _seatService;
    private readonly ISeatTypeService _seatTypeService;
    private readonly IManagerCinemaScopeService _managerCinemaScopeService;

    public SeatController(
        ISeatService seatService,
        ISeatTypeService seatTypeService,
        IManagerCinemaScopeService managerCinemaScopeService)
    {
        _seatService = seatService;
        _seatTypeService = seatTypeService;
        _managerCinemaScopeService = managerCinemaScopeService;
    }

    [HttpGet("seats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeats(
        int roomId,
        CancellationToken cancellationToken)
    {
        var seats = await _seatService.GetSeatsByRoomAsync(roomId, cancellationToken);
        if (seats is null)
        {
            return NotFound(new { success = false, message = "Room not found" });
        }

        return Ok(seats.Select(ToResponse));
    }

    [HttpGet("seats/{seatId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeat(
        int roomId,
        int seatId,
        CancellationToken cancellationToken)
    {
        var seat = await _seatService.GetSeatByIdAsync(roomId, seatId, cancellationToken);

        return seat is null
            ? NotFound(new { success = false, message = "Seat not found" })
            : Ok(ToResponse(seat));
    }

    [HttpPost("seats")]
    public async Task<IActionResult> CreateSeat(
        int roomId,
        [FromBody] SeatRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _seatService.CreateSeatAsync(
            roomId,
            request.RowLabel,
            request.SeatNumber,
            request.SeatTypeId,
            request.Status,
            request.IsGap,
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        var response = ToResponse(result.Seat!);

        return CreatedAtAction(
            nameof(GetSeat),
            new { roomId, seatId = response.SeatId },
            response);
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

        return Ok(ToGenerateResponse(roomId, result.Result!));
    }

    [HttpPatch("seats/{seatId:int}")]
    public async Task<IActionResult> UpdateSeat(
        int roomId,
        int seatId,
        [FromBody] SeatUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _seatService.UpdateSeatAsync(
            roomId,
            seatId,
            request.SeatTypeId,
            request.Status,
            request.IsGap,
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        return Ok(ToResponse(result.Seat!));
    }

    [HttpDelete("seats/{seatId:int}")]
    public async Task<IActionResult> DeleteSeat(
        int roomId,
        int seatId,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _seatService.DeleteSeatAsync(
            roomId, seatId, managerCinemaId, cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        return NoContent();
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
            ToAppSelector(request.Selector),
            request.Update.SeatTypeId,
            request.Update.Status,
            request.Update.IsGap,
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        return Ok(result.Seats.Select(ToResponse));
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
            ToAppSelector(request.Selector),
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("seat-map")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeatMap(
        int roomId,
        CancellationToken cancellationToken)
    {
        var seats = await _seatService.GetSeatsByRoomAsync(roomId, cancellationToken);
        if (seats is null)
        {
            return NotFound(new { success = false, message = "Room not found" });
        }

        return Ok(ToSeatMapResponse(roomId, seats));
    }

    [HttpGet("layout")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeatLayout(
        int roomId,
        CancellationToken cancellationToken)
    {
        var layout = await _seatService.GetLayoutAsync(roomId, cancellationToken);
        if (layout is null)
        {
            return NotFound(new { success = false, message = "Room not found" });
        }

        return Ok(ToLayoutResponse(roomId, layout));
    }

    [HttpPut("layout")]
    public async Task<IActionResult> ReplaceSeatLayout(
        int roomId,
        [FromBody] SeatLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var managerCinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!managerCinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _seatService.ReplaceLayoutAsync(
            roomId,
            request.TotalRows,
            request.TotalCols,
            request.Seats?.Select(ToSeatLayoutSeatItem).ToList() ?? [],
            managerCinemaId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return await ToErrorResponseAsync(result.ErrorMessage, cancellationToken);
        }

        return Ok(ToLayoutResponse(
            roomId,
            new SeatLayoutResult(
                request.TotalRows,
                request.TotalCols,
                result.Seats)));
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

    private static SeatLayoutResponse ToLayoutResponse(
        int roomId,
        SeatLayoutResult layout)
    {
        return new SeatLayoutResponse(
            roomId,
            layout.TotalRows,
            layout.TotalCols,
            layout.Seats.Select(ToResponse).ToList());
    }

    private static SeatLayoutSeatItem ToSeatLayoutSeatItem(SeatLayoutSeatRequest request)
    {
        return new SeatLayoutSeatItem(
            request.RowLabel,
            request.ColIndex,
            request.SeatName,
            request.SeatTypeId,
            request.Status,
            request.IsGap);
    }

    private static SeatGenerateResponse ToGenerateResponse(
        int roomId,
        SeatGenerateResult result)
    {
        return new SeatGenerateResponse(
            roomId,
            result.Rows,
            result.Columns,
            result.Seats.Select(ToResponse).ToList());
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
