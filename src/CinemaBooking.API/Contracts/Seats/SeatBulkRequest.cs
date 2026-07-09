namespace CinemaBooking.API.Contracts.Seats;

public sealed class SeatBulkRequest
{
    public SeatSelector? Selector { get; init; }
    public IReadOnlyCollection<SeatSelector>? Selectors { get; init; }
    public SeatBulkUpdate Update { get; init; } = null!;
}
