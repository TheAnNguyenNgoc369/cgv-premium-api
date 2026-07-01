using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CinemaBooking.API.Contracts.Showtimes;

namespace CinemaBooking.API.Tests;

public sealed class ShowtimeRequestContractTests
{
    [Fact]
    public void CreateRequest_VietnamOffset_DeserializesAndValidates()
    {
        const string json = """
            {
              "movieId": 1,
              "roomId": 2,
              "startTime": "2026-07-01T15:58:59.838+07:00",
              "basePrice": 100000
            }
            """;

        var request = JsonSerializer.Deserialize<CreateShowtimeRequest>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal(TimeSpan.FromHours(7), request.StartTime.Offset);
        Assert.Equal(
            new DateTime(2026, 7, 1, 8, 58, 59, 838, DateTimeKind.Utc),
            request.StartTime.UtcDateTime);
        Assert.Empty(Validate(request));
    }

    [Fact]
    public void CreateRequest_UtcOffset_ReturnsStartTimeValidationErrorOnly()
    {
        var request = new CreateShowtimeRequest(
            1,
            2,
            new DateTimeOffset(2026, 7, 1, 8, 58, 59, TimeSpan.Zero),
            100000);

        var errors = Validate(request);

        var error = Assert.Single(errors);
        Assert.Equal(
            "StartTime must use ISO 8601 Vietnam time with the +07:00 offset",
            error.ErrorMessage);
        Assert.Contains(nameof(CreateShowtimeRequest.StartTime), error.MemberNames);
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        return results;
    }
}
