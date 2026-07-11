using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers.RuleEngine.Validators;

/// <summary>
/// Validates DayOfWeek rules based on showtime
/// </summary>
public sealed class DayOfWeekValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.DayOfWeek;

    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredDayOfWeek = rule.RuleValue;
        var showtimeDayOfWeek = context.ShowtimeDateTime.DayOfWeek.ToString();

        if (!string.Equals(showtimeDayOfWeek, requiredDayOfWeek, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure(
                RuleType,
                $"Voucher is only valid on {requiredDayOfWeek}.");
        }

        return ValidationResult.Success(0);
    }
}
