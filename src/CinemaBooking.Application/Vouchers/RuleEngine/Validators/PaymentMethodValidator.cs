using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers.RuleEngine.Validators;

/// <summary>
/// Validates PaymentMethod rules
/// </summary>
public sealed class PaymentMethodValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.PaymentMethod;

    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredMethod = rule.RuleValue;
        var currentMethod = context.PaymentMethod;

        if (!string.Equals(currentMethod, requiredMethod, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure(
                RuleType,
                $"Voucher is only valid for {requiredMethod} payments.");
        }

        return ValidationResult.Success(0);
    }
}
