using System.Text.Json.Nodes;
using CinemaBooking.API.Contracts.AdminUsers;
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

            case Type type when type == typeof(CreateAdminUserRequest):
                SetExample(schema, "fullName", "Nguyen Van A");
                SetExample(schema, "email", "user@example.com");
                SetExample(schema, "phone", "0901234567");
                SetExample(schema, "password", "Password@123");
                SetExample(schema, "role", "staff");
                SetExample(schema, "status", "active");
                SetExample(schema, "cinemaId", 1);
                break;

            case Type type when type == typeof(UpdateAdminUserRequest):
                SetExample(schema, "fullName", "Nguyen Van A");
                SetExample(schema, "email", "user@example.com");
                SetExample(schema, "phone", "0901234567");
                SetExample(schema, "cinemaId", 1);
                break;

            case Type type when type == typeof(ChangeUserRoleRequest):
                SetExample(schema, "role", "staff");
                SetExample(schema, "cinemaId", 1);
                break;

            case Type type when type == typeof(ChangeUserStatusRequest):
                SetExample(schema, "status", "active");
                break;

            case Type type when type == typeof(ResetUserPasswordRequest):
                SetExample(schema, "password", "Password@123");
                SetExample(schema, "confirmPassword", "Password@123");
                break;
        }
    }

    private static void SetExample(
        IOpenApiSchema schema,
        string propertyName,
        string example) => SetExample(schema, propertyName, JsonValue.Create(example));

    private static void SetExample(
        IOpenApiSchema schema,
        string propertyName,
        int example) => SetExample(schema, propertyName, JsonValue.Create(example));

    private static void SetExample(
        IOpenApiSchema schema,
        string propertyName,
        JsonNode? example)
    {
        if (schema.Properties is not null
            && schema.Properties.TryGetValue(propertyName, out var property)
            && property is OpenApiSchema openApiProperty)
        {
            openApiProperty.Example = example;
        }
    }
}
