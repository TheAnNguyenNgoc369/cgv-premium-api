using CinemaBooking.API.Controllers;

namespace CinemaBooking.API.Tests;

public sealed class SeatEndpointContractTests
{
    [Theory]
    [InlineData("CreateSeat")]
    [InlineData("GetSeat")]
    [InlineData("GetSeatMap")]
    [InlineData("UpdateSeat")]
    [InlineData("DeleteSeat")]
    [InlineData("GetSeatLayout")]
    [InlineData("ReplaceSeatLayout")]
    public void ObsoleteSeatEndpoint_IsNotExposed(string methodName)
    {
        Assert.Null(typeof(SeatController).GetMethod(methodName));
    }

    [Theory]
    [InlineData("GetSeats")]
    [InlineData("GenerateSeats")]
    [InlineData("BulkUpdateSeats")]
    [InlineData("BulkDeleteSeats")]
    public void RequiredSeatEndpoint_IsExposed(string methodName)
    {
        Assert.NotNull(typeof(SeatController).GetMethod(methodName));
    }
}
