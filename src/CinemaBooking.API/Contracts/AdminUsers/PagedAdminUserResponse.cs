namespace CinemaBooking.API.Contracts.AdminUsers;

public sealed record PagedAdminUserResponse(
    IReadOnlyList<AdminUserListResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
