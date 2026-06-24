namespace CinemaBooking.Application.Common.Enums;

public sealed record EnumValidationResult(
    bool Succeeded,
    string? DatabaseValue,
    string? ErrorMessage);
