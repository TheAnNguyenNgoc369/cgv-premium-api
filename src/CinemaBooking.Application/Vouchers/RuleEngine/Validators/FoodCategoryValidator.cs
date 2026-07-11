using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers.RuleEngine.Validators;

/// <summary>
/// Validates FoodCategory rules - booking must contain products from required category
/// </summary>
public sealed class FoodCategoryValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.FoodCategory;

    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredCategory = rule.RuleValue;
        var products = context.Products;

        var hasCategory = products.Any(p =>
            !string.IsNullOrEmpty(p.Category)
            && string.Equals(p.Category, requiredCategory, StringComparison.OrdinalIgnoreCase));

        if (!hasCategory)
        {
            return ValidationResult.Failure(
                RuleType,
                "Voucher requires products from a specific category that are not in this booking.");
        }

        return ValidationResult.Success(0);
    }
}
