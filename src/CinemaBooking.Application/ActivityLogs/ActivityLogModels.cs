namespace CinemaBooking.Application.ActivityLogs;

public sealed record ActivityLogActor(int UserId, string FullName, string Role);
public sealed record ActivityLogItem(int LogId, DateTime Timestamp, ActivityLogActor Actor,
    string ActionType, string Module, string Description, string IpAddress,
    int? TargetUserId, string? TargetTable, int? TargetId);
public sealed record ActivityLogDetail(int LogId, DateTime CreatedAt, int ActorId,
    string ActorName, string ActorRole, string ActionType, string Module, string IpAddress,
    int? TargetUserId, string? TargetTable, int? TargetId, string Description);
public sealed record ActivityLogPage(IReadOnlyList<ActivityLogItem> Items, int Page,
    int PageSize, int TotalItems, int TotalPages);
