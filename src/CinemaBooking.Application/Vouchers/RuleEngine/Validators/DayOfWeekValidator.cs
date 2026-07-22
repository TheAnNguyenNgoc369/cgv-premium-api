using System.Text;
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

        // TEMP DEBUG — DoW voucher trace. Remove after diagnosis.
        var equalsResult = string.Equals(showtimeDayOfWeek, requiredDayOfWeek, StringComparison.OrdinalIgnoreCase);
        var ruleBytesHex = BitConverter.ToString(Encoding.UTF8.GetBytes(requiredDayOfWeek ?? string.Empty));
        var showtimeBytesHex = BitConverter.ToString(Encoding.UTF8.GetBytes(showtimeDayOfWeek));
        Console.WriteLine("===== DOW_DEBUG BEGIN =====");
        Console.WriteLine($"DOW_DEBUG VoucherID  = {rule.VoucherID}");
        Console.WriteLine($"DOW_DEBUG RuleID     = {rule.RuleID}");
        Console.WriteLine($"DOW_DEBUG RuleValue  = \"{requiredDayOfWeek}\"");
        Console.WriteLine($"DOW_DEBUG RuleValue.Length = {(requiredDayOfWeek?.Length ?? 0)}");
        Console.WriteLine($"DOW_DEBUG RuleValue UTF-8 bytes = {ruleBytesHex}");
        Console.WriteLine($"DOW_DEBUG CurrentShowtime          = {context.ShowtimeDateTime:o}");
        Console.WriteLine($"DOW_DEBUG CurrentShowtime.Kind     = {context.ShowtimeDateTime.Kind}");
        Console.WriteLine($"DOW_DEBUG CurrentShowtime.DayOfWeek = {context.ShowtimeDateTime.DayOfWeek}");
        Console.WriteLine($"DOW_DEBUG DayOfWeek.ToString()     = \"{showtimeDayOfWeek}\"");
        Console.WriteLine($"DOW_DEBUG DayOfWeek UTF-8 bytes    = {showtimeBytesHex}");
        Console.WriteLine($"DOW_DEBUG EqualsResult             = {equalsResult}");
        Console.WriteLine("===== DOW_DEBUG END =====");

        if (!string.Equals(showtimeDayOfWeek, requiredDayOfWeek, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure(
                RuleType,
                $"Voucher is only valid on {requiredDayOfWeek}.");
        }

        return ValidationResult.Success(0);
    }
}
