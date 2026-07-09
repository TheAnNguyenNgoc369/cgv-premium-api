using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Validation;

public sealed class ValidItemTypeAttribute : ValidationAttribute
{
    private static readonly string[] ValidValues = ["combo", "snack", "beverage", "dessert"];

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var itemType = value.ToString()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(itemType))
        {
            return ValidationResult.Success;
        }

        if (!ValidValues.Contains(itemType))
        {
            return new ValidationResult(
                $"ItemType must be one of: {string.Join(", ", ValidValues)}");
        }

        return ValidationResult.Success;
    }
}
