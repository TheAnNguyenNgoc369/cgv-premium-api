namespace CinemaBooking.API.Contracts.Refunds;

public sealed record RefundDetailResponse(
    int RefundId,
    int BookingId,
    string BookingCode,
    string MovieTitle,
    DateTime ShowtimeStartTime,
    string CinemaName,
    string RoomName,
    decimal RefundAmount,
    string Reason,
    string Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    string? ProcessedByName
);
