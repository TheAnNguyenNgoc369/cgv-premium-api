using System.Text.Json;
using CinemaBooking.API.Contracts.Membership;

namespace CinemaBooking.API.Tests;

public sealed class MembershipContractTests
{
    [Fact]
    public void MembershipResponse_UsesSnakeCaseRefundFields()
    {
        var response = new MembershipResponse(
            CurrentTier: "gold",
            NextTier: "platinum",
            PointsToNextTier: 100,
            TotalPoints: 900,
            TotalSpent: 1_000_000m,
            DiscountPercent: 5m,
            TotalRefunds: 3,
            UsedRefunds: 1);

        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"total_refunds\":3", json);
        Assert.Contains("\"used_refunds\":1", json);
    }

    [Fact]
    public void TierResponse_UsesSnakeCaseRefundLimitField()
    {
        var response = new TierResponse(
            TierID: 2,
            TierName: "gold",
            MinPoints: 1000,
            DiscountRate: 0.05m,
            TotalRefunds: 3);

        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"total_refunds\":3", json);
    }
}
