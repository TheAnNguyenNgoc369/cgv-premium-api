using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.ShowtimeTypes;

public sealed record ShowtimeTypePage(IReadOnlyList<ShowtimeType> Items, int TotalItems);
public sealed record ShowtimeTypeItem(DateOnly Date, DateTime StartTime, DateTime EndTime,
    bool IsConflict, string? ConflictCode, string? Reason, string Status = "valid");
public sealed record ShowtimeTypeBatchResult(bool Succeeded, string? ErrorMessage,
    IReadOnlyList<ShowtimeTypeItem> Items, int ValidCount, int ConflictCount);
public sealed record ShowtimeTypeWriteResult(bool Succeeded, string? ErrorMessage, ShowtimeType? Value);
