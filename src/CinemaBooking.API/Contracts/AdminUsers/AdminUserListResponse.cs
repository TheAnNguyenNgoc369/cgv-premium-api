namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed record AdminUserListResponse(
    int UserId,
    string FullName,
    string Email,
    string Phone,
    string Role,
    string Status,
    int? CinemaId,
    string? AvatarUrl,
    int TotalPoints,
    string? MembershipTier,
    DateTime CreatedAt,
    DateTime UpdatedAt);
