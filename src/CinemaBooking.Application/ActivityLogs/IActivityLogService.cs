namespace CinemaBooking.Application.ActivityLogs;

public interface IActivityLogService
{
    Task<ActivityLogPage> GetAsync(string? actionType, string? module, int? actorId,
        int? targetUserId, string? targetTable, int? targetId, DateOnly? startDate,
        DateOnly? endDate, int page, int pageSize, CancellationToken cancellationToken);
    Task<ActivityLogDetail?> GetByIdAsync(int id, CancellationToken cancellationToken);
    IReadOnlyList<string> GetActionTypes();
}
