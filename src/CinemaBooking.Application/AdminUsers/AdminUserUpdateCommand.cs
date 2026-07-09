namespace CinemaBooking.Application.AdminUsers;

public sealed record AdminUserUpdateCommand(
    string FullName,
    string Email,
    string Phone,
    int? CinemaId);
