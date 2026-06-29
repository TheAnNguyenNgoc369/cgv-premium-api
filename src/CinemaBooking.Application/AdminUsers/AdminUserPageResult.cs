using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.AdminUsers;

public sealed record AdminUserPageResult(
    IReadOnlyList<User> Items,
    int Page,
    int PageSize,
    int TotalItems);
