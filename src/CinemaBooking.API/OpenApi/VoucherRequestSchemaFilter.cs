using System.Text.Json.Nodes;
using CinemaBooking.API.Contracts.Vouchers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CinemaBooking.API.OpenApi;

public sealed class VoucherRequestSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(VoucherRequest))
            return;

        SetExample(schema, "voucherCode", "SUMMER10");
        SetExample(schema, "discountType", "percent");
        SetExample(schema, "discountValue", 10);
        SetExample(schema, "minOrderValue", 100000);
        SetExample(schema, "maxUses", 100);
        SetExample(schema, "validFrom", "2026-07-01T00:00:00+07:00");
        SetExample(schema, "validUntil", "2026-08-01T00:00:00+07:00");
        SetExample(schema, "description", "Summer promotion 10% off orders over 100k VND");
        SetExample(schema, "isActive", true);
        // Public voucher example: used directly via VoucherCode. RequiredPoints and
        // ExchangeLimit must be null. Set isRedeemable=true with RequiredPoints/ExchangeLimit
        // > 0 for loyalty (points-redeemed) vouchers.
        SetExample(schema, "isRedeemable", false);
        SetNullExample(schema, "requiredPoints");
        SetNullExample(schema, "exchangeLimit");
    }

    private static void SetExample(IOpenApiSchema schema, string propertyName, string example) =>
        SetExample(schema, propertyName, JsonValue.Create(example));

    private static void SetExample(IOpenApiSchema schema, string propertyName, int example) =>
        SetExample(schema, propertyName, JsonValue.Create(example));

    private static void SetExample(IOpenApiSchema schema, string propertyName, bool example) =>
        SetExample(schema, propertyName, JsonValue.Create(example));

    private static void SetNullExample(IOpenApiSchema schema, string propertyName) =>
        SetExample(schema, propertyName, (JsonNode?)null);

    private static void SetExample(IOpenApiSchema schema, string propertyName, JsonNode? example)
    {
        if (schema.Properties is not null
            && schema.Properties.TryGetValue(propertyName, out var property)
            && property is OpenApiSchema openApiProperty)
        {
            openApiProperty.Example = example;
        }
    }
}
