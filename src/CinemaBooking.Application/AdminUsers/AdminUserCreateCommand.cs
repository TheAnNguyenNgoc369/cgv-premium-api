namespace CinemaBooking.Application.AdminUsers;

public sealed record AdminUserCreateCommand(
    string FullName,
    string Email,
    string Phone,
    string Password,
    string Role,
    string? Status,
    int? CinemaId);
