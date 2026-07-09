using System.Text.Json.Nodes;
using CinemaBooking.API.Contracts.Showtimes;
using CinemaBooking.Shared.Time;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CinemaBooking.API.OpenApi;

public sealed class ShowtimeRequestSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(CreateShowtimeRequest)
            && context.Type != typeof(UpdateShowtimeRequest))
            return;

        if (schema.Properties is null
            || !schema.Properties.TryGetValue("startTime", out var property)
            || property is not OpenApiSchema startTimeProperty)
            return;

        var vietnamNow = VietnamTime.FromUtc(DateTime.UtcNow);
        var example = new DateTimeOffset(
            vietnamNow.Year,
            vietnamNow.Month,
            vietnamNow.Day,
            19,
            30,
            0,
            VietnamTime.UtcOffset).AddDays(1);

        startTimeProperty.Example = JsonValue.Create(example.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
        startTimeProperty.Description = "Vietnam time in ISO 8601 format with the required +07:00 offset.";
    }
}
