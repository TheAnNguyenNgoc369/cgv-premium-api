namespace CinemaBooking.Application.Common.Enums;

public static class EnumValueMapper
{
    public static EnumValidationResult Validate(
        string? value,
        string fieldName,
        IReadOnlyDictionary<string, string> mappings)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new(false, null, $"{fieldName} is required");

        var apiValue = value.Trim().ToUpperInvariant();
        if (mappings.TryGetValue(apiValue, out var databaseValue))
            return new(true, databaseValue, null);

        return new(
            false,
            null,
            $"{fieldName} must be one of: {string.Join(", ", mappings.Keys)}");
    }

    public static string ToApiValue(string databaseValue) =>
        databaseValue.Trim().ToUpperInvariant();

    public static IReadOnlyDictionary<string, string> CreateMappings(params string[] databaseValues) =>
        databaseValues.ToDictionary(
            value => value.ToUpperInvariant(),
            value => value,
            StringComparer.OrdinalIgnoreCase);
}
