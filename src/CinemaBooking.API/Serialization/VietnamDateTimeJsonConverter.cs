using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CinemaBooking.Shared.Time;

namespace CinemaBooking.API.Serialization;

public sealed class VietnamDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Date-time value must be an ISO 8601 string with an explicit offset.");

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value)
            || !HasExplicitOffset(value)
            || !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out var dateTimeOffset))
            throw new JsonException("Date-time value must use ISO 8601 format with an explicit offset.");

        return dateTimeOffset.UtcDateTime;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(VietnamTime.FromUtc(value));

    private static bool HasExplicitOffset(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            return true;

        var timeSeparator = trimmed.IndexOf('T');
        return timeSeparator >= 0
            && (trimmed.LastIndexOf('+') > timeSeparator
                || trimmed.LastIndexOf('-') > timeSeparator);
    }
}
