namespace CinemaBooking.API.Contracts.Persons;

public sealed class UpdatePersonRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Biography { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? Gender { get; init; }
    public string? PhotoUrl { get; init; }
    public string? PhotoPublicId { get; init; }
}
