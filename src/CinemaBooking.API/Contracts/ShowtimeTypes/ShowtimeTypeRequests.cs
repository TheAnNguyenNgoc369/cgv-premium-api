using System.ComponentModel.DataAnnotations;
namespace CinemaBooking.API.Contracts.ShowtimeTypes;
public sealed record CreateShowtimeTypeRequest(
    [param: Range(1, int.MaxValue, ErrorMessage = "cinemaId must be greater than 0.")] int CinemaId,
    [param: Required(ErrorMessage = "name is required."), MaxLength(100, ErrorMessage = "name must not exceed 100 characters.")] string Name,
    [param: Required(ErrorMessage = "slots is required.")] IReadOnlyList<TimeSpan> Slots);
public sealed record UpdateShowtimeTypeRequest(
    [param: Required(ErrorMessage = "name is required."), MaxLength(100, ErrorMessage = "name must not exceed 100 characters.")] string Name,
    bool IsActive,
    [param: Required(ErrorMessage = "slots is required.")] IReadOnlyList<TimeSpan> Slots);
public sealed record ShowtimeTypeBatchRequest(
    [param: Range(1, int.MaxValue, ErrorMessage = "movieId must be greater than 0.")] int MovieId,
    [param: Range(1, int.MaxValue, ErrorMessage = "roomId must be greater than 0.")] int RoomId,
    DateOnly StartDate,
    DateOnly EndDate,
    [param: Range(1, int.MaxValue, ErrorMessage = "showtimeTypeId must be greater than 0.")] int ShowtimeTypeId,
    [param: Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "basePrice must be greater than or equal to 0.")] decimal BasePrice);
