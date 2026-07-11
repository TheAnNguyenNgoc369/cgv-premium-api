namespace CinemaBooking.Application.Vouchers.RuleEngine;

/// <summary>
/// Service for validating vouchers against their configured rules
/// </summary>
public interface IVoucherRuleEngine
{
    /// <summary>
    /// Validates whether a voucher can be applied to a booking based on its rules
    /// </summary>
    /// <param name="context">Validation context containing booking and voucher data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with applicable amount if successful</returns>
    Task<ValidationResult> ValidateAsync(VoucherValidationContext context, CancellationToken cancellationToken = default);
}
