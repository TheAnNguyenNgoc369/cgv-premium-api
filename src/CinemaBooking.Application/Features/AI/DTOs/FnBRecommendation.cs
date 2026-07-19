namespace CinemaBooking.Application.Features.AI.DTOs;

public sealed record FnBRecommendation
{
    public int ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}
