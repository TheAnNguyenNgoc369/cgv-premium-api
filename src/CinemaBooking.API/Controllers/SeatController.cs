using CinemaBooking.API.Contracts.Seats;
using CinemaBooking.Application.Seats;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/rooms/{roomId:int}")]
[Authorize(Roles = Roles.Manager)]
public sealed class SeatController : ControllerBase
{
    private readonly ISeatService _seatService;

    public SeatController(ISeatService seatService)
    {
        _seatService = seatService;
    }

    [HttpGet("seats")]
    public async Task<IActionResult> GetSeats(
        int roomId,
        CancellationToken cancellationToken)
    {
        var seats = await _seatService.GetSeatsByRoomAsync(roomId, cancellationToken);
        if (seats is null)
        {
            return NotFound(new { message = "Room not found" });
        }

        return Ok(seats.Select(ToResponse));
    }

    [HttpPost("seats")]
    public async Task<IActionResult> CreateSeat(
        int roomId,
        [FromBody] SeatRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _seatService.CreateSeatAsync(
            roomId,
            request.RowLabel,
            request.SeatNumber,
            request.SeatCode,
            request.Type,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResponse(result.ErrorMessage);
        }

        var response = ToResponse(result.Seat!);

        return CreatedAtAction(
            nameof(GetSeats),
            new { roomId },
            response);
    }

    [HttpPut("seats/{seatId:int}")]
    public async Task<IActionResult> UpdateSeat(
        int roomId,
        int seatId,
        [FromBody] SeatUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _seatService.UpdateSeatAsync(
            roomId,
            seatId,
            request.Type,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResponse(result.ErrorMessage);
        }

        return Ok(ToResponse(result.Seat!));
    }

    [HttpDelete("seats/{seatId:int}")]
    public async Task<IActionResult> DeleteSeat(
        int roomId,
        int seatId,
        CancellationToken cancellationToken)
    {
        var result = await _seatService.DeleteSeatAsync(roomId, seatId, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResponse(result.ErrorMessage);
        }

        return NoContent();
    }

    [HttpPut("seat-layout")]
    public async Task<IActionResult> ReplaceSeatLayout(
        int roomId,
        [FromBody] SeatLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _seatService.ReplaceLayoutAsync(
            roomId,
            request.TotalRows,
            request.SeatsPerRow,
            request.SeatType,
            request.SeatStatus,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResponse(result.ErrorMessage);
        }

        return Ok(result.Seats.Select(ToResponse));
    }

    private IActionResult ToErrorResponse(string? errorMessage)
    {
        if (errorMessage is "Room not found" or "Seat not found")
        {
            return NotFound(new { message = errorMessage });
        }

        if (errorMessage is "Room has active or upcoming schedules"
            or "Seat has related booking or hold records")
        {
            return Conflict(new { message = errorMessage });
        }

        return BadRequest(new { message = errorMessage });
    }

    private static SeatResponse ToResponse(Seat seat)
    {
        return new SeatResponse(
            seat.SeatID,
            seat.RoomID,
            seat.SeatRow,
            seat.SeatCol,
            $"{seat.SeatRow}{seat.SeatCol}",
            EnumValueMapper.ToApiValue(seat.SeatType.TypeName),
            EnumValueMapper.ToApiValue(seat.Status));
    }
}
