using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Repositories;

namespace CinemaBooking.API.Tests;

public sealed class SeatRepositoryLayoutTests
{
    [Fact]
    public void ApplyLayoutValues_ExistingSeatBecomesGap_UpdatesGapInvariant()
    {
        var existingSeat = new Seat
        {
            SeatID = 1,
            SeatTypeID = 10,
            Status = "active",
            IsGap = false
        };
        var requestedGap = new Seat
        {
            SeatTypeID = null,
            Status = "inactive",
            IsGap = true
        };

        SeatRepository.ApplyLayoutValues(existingSeat, requestedGap);

        Assert.True(existingSeat.IsGap);
        Assert.Null(existingSeat.SeatTypeID);
        Assert.Equal("inactive", existingSeat.Status);
    }
}
