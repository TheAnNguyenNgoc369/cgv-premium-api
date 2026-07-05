namespace CinemaBooking.API.Contracts.Seats;

public sealed class SeatBulkDeleteRequest
{
    public SeatSelector? Selector { get; init; }
    public IReadOnlyCollection<SeatSelector>? Selectors { get; init; }
}
