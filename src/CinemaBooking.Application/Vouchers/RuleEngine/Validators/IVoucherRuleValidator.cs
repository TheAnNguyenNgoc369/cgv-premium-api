using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Vouchers.RuleEngine.Validators;

/// <summary>
/// Base interface for voucher rule validators
/// </summary>
public interface IVoucherRuleValidator
{
    /// <summary>
    /// Rule type that this validator handles
    /// </summary>
    string RuleType { get; }

    /// <summary>
    /// Validates a specific voucher rule against the booking context
    /// </summary>
    /// <param name="rule">The voucher rule to validate</param>
    /// <param name="context">Validation context containing booking data</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(VoucherRule rule, VoucherValidationContext context);
}
