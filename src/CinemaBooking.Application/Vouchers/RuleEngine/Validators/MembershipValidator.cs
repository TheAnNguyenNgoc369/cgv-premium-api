using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers.RuleEngine.Validators;

/// <summary>
/// Validates Membership rules - user must have required membership tier
/// </summary>
public sealed class MembershipValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.Membership;

    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredMembership = rule.RuleValue;

        if (string.IsNullOrWhiteSpace(context.MembershipTier))
        {
            return ValidationResult.Failure(
                RuleType,
                "Voucher requires membership but user is not authenticated or has no membership tier.");
        }

        if (context.MembershipTier != requiredMembership)
        {
            return ValidationResult.Failure(
                RuleType,
                $"Voucher is only valid for {requiredMembership} members.");
        }

        return ValidationResult.Success(0);
    }
}
