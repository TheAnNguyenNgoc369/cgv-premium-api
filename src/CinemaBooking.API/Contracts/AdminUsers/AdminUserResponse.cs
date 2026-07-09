namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed record AdminUserResponse(
    int UserId,
    string FullName,
    string Email,
    string Phone,
    string Role,
    string Status,
    int? CinemaId,
    string? AvatarUrl,
    int TotalPoints,
    DateTime CreatedAt,
    DateTime UpdatedAt);
