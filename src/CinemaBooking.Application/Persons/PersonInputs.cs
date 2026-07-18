namespace CinemaBooking.Application.Persons;

public sealed record CreatePersonInput(
    string Name,
    string? Biography,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? Gender,
    string? PhotoUrl,
    string? PhotoPublicId);

public sealed record UpdatePersonInput(
    string Name,
    string? Biography,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? Gender,
    string? PhotoUrl,
    string? PhotoPublicId);
