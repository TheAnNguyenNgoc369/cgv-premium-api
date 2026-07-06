using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CinemaBooking.API.Contracts.Cinemas;

namespace CinemaBooking.API.Tests;

public sealed class CinemaCoordinateContractTests
{
    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void CinemaRequest_CoordinatesAtRangeBoundaries_AreValid(
        double latitude,
        double longitude)
    {
        Assert.True(GetRange(nameof(CinemaRequest.Latitude)).IsValid(latitude));
        Assert.True(GetRange(nameof(CinemaRequest.Longitude)).IsValid(longitude));
    }

    [Theory]
    [InlineData(-90.000001, 0)]
    [InlineData(90.000001, 0)]
    [InlineData(0, -180.000001)]
    [InlineData(0, 180.000001)]
    public void CinemaRequest_CoordinatesOutsideRanges_AreInvalid(
        double latitude,
        double longitude)
    {
        var latitudeIsValid = GetRange(nameof(CinemaRequest.Latitude)).IsValid(latitude);
        var longitudeIsValid = GetRange(nameof(CinemaRequest.Longitude)).IsValid(longitude);

        Assert.False(latitudeIsValid && longitudeIsValid);
    }

    [Fact]
    public void CinemaRequest_NullCoordinates_AreValid()
    {
        Assert.True(GetRange(nameof(CinemaRequest.Latitude)).IsValid(null));
        Assert.True(GetRange(nameof(CinemaRequest.Longitude)).IsValid(null));
    }

    private static RangeAttribute GetRange(string parameterName) =>
        typeof(CinemaRequest).GetConstructors().Single()
            .GetParameters().Single(parameter => parameter.Name == parameterName)
            .GetCustomAttribute<RangeAttribute>()
        ?? throw new InvalidOperationException($"RangeAttribute missing from {parameterName} parameter.");
}
