using System.Text.Json.Nodes;
using CinemaBooking.API.Contracts.Auth;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CinemaBooking.API.OpenApi;

public sealed class AuthRequestSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        switch (context.Type)
        {
            case Type type when type == typeof(RegisterRequest):
                SetExample(schema, "password", "Password1!");
                SetExample(schema, "confirmPassword", "Password1!");
                break;

            case Type type when type == typeof(LoginRequest):
                SetExample(schema, "password", "Password1!");
                break;

            case Type type when type == typeof(ResetPasswordRequest):
                SetExample(schema, "newPassword", "Password1!");
                SetExample(schema, "confirmPassword", "Password1!");
                break;
        }
    }

    private static void SetExample(
        IOpenApiSchema schema,
        string propertyName,
        string example)
    {
        if (schema.Properties is not null
            && schema.Properties.TryGetValue(propertyName, out var property)
            && property is OpenApiSchema openApiProperty)
        {
            openApiProperty.Example = JsonValue.Create(example);
        }
    }
}
