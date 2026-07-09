using System.Text.Json;
using CinemaBooking.API.Serialization;
using CinemaBooking.Shared.Time;

namespace CinemaBooking.API.Tests;

public sealed class VietnamTimeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new VietnamDateTimeJsonConverter() }
    };

    [Fact]
    public void Serialize_UtcDateTime_WritesVietnamOffset()
    {
        var utc = new DateTime(2026, 7, 5, 12, 30, 0, DateTimeKind.Utc);

        var json = JsonSerializer.Serialize(utc, JsonOptions);

        Assert.Equal("\"2026-07-05T19:30:00+07:00\"", json);
    }

    [Fact]
    public void Deserialize_VietnamDateTime_NormalizesToUtc()
    {
        var result = JsonSerializer.Deserialize<DateTime>(
            "\"2026-07-05T19:30:00+07:00\"", JsonOptions);

        Assert.Equal(new DateTime(2026, 7, 5, 12, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Deserialize_UtcDateTime_PreservesUtcInstant()
    {
        var result = JsonSerializer.Deserialize<DateTime>(
            "\"2026-07-05T12:30:00Z\"", JsonOptions);

        Assert.Equal(new DateTime(2026, 7, 5, 12, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Deserialize_DateTimeWithoutOffset_RejectsAmbiguousLocalTime()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(
            "\"2026-07-05T19:30:00\"", JsonOptions));
    }

    [Fact]
    public void GetUtcDayRange_UsesVietnamCalendarDay()
    {
        var (fromUtc, toUtc) = VietnamTime.GetUtcDayRange(new DateOnly(2026, 7, 5));

        Assert.Equal(new DateTime(2026, 7, 4, 17, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 7, 5, 17, 0, 0, DateTimeKind.Utc), toUtc);
    }

    [Fact]
    public void GetDate_AroundUtcMidnight_UsesVietnamDate()
    {
        var utc = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc);

        var result = VietnamTime.GetDate(utc);

        Assert.Equal(new DateOnly(2026, 7, 2), result);
    }
}
