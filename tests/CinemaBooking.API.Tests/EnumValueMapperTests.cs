using CinemaBooking.Application.Common.Enums;

namespace CinemaBooking.API.Tests;

public sealed class EnumValueMapperTests
{
    [Theory]
    [InlineData(" ACTIVE ", "active")]
    [InlineData("active", "active")]
    [InlineData("Maintenance", "maintenance")]
    public void ValidateMapsCaseInsensitiveApiValueToDatabaseValue(
        string input,
        string expected)
    {
        var result = EnumValueMapper.Validate(
            input, "Status", DatabaseEnumMappings.CinemaStatuses);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.DatabaseValue);
    }

    [Fact]
    public void ValidateReturnsAvailableApiValuesForInvalidInput()
    {
        var result = EnumValueMapper.Validate(
            "unknown", "PaymentMethod", DatabaseEnumMappings.PaymentMethods);

        Assert.False(result.Succeeded);
        Assert.Contains("VNPAY", result.ErrorMessage);
        Assert.Contains("CREDIT_CARD", result.ErrorMessage);
        Assert.Contains("WALLET", result.ErrorMessage);
    }

    [Fact]
    public void RoomTypeMapsThreeDToDatabaseConstraintValue()
    {
        var result = EnumValueMapper.Validate(
            "THREE_D", "Type", DatabaseEnumMappings.RoomTypes);

        Assert.True(result.Succeeded);
        Assert.Equal("3D", result.DatabaseValue);
    }
}
