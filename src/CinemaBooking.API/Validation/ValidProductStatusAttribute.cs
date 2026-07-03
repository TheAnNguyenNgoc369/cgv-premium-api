using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Validation;

public sealed class ValidProductStatusAttribute : ValidationAttribute
{
    private static readonly string[] ValidValues = ["active", "inactive"];

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var status = value.ToString()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(status))
        {
            return ValidationResult.Success;
        }

        if (!ValidValues.Contains(status))
        {
            return new ValidationResult(
                $"Status must be one of: {string.Join(", ", ValidValues)}");
        }

        return ValidationResult.Success;
    }
}
