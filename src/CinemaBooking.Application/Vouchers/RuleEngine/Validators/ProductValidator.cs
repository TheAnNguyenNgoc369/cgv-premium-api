using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers.RuleEngine.Validators;

/// <summary>
/// Validates Product rules - booking must contain the required product
/// </summary>
public sealed class ProductValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.Product;

    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredProductId = rule.RuleValue;
        var products = context.Products;

        var hasProduct = products.Any(p => p.ProductID.ToString() == requiredProductId);

        if (!hasProduct)
        {
            return ValidationResult.Failure(
                RuleType,
                "Voucher requires a specific product that is not in this booking.");
        }

        return ValidationResult.Success(0);
    }
}
