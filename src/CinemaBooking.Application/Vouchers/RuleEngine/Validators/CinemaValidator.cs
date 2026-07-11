using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers.RuleEngine.Validators;

/// <summary>
/// Validates Cinema rules
/// </summary>
public sealed class CinemaValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.Cinema;

    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredCinemaId = rule.RuleValue;
        var bookingCinemaId = context.CinemaId.ToString();

        if (requiredCinemaId != bookingCinemaId)
        {
            return ValidationResult.Failure(
                RuleType,
                "Voucher is not valid for this cinema.");
        }

        return ValidationResult.Success(0);
    }
}
