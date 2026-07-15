using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Vouchers.RuleEngine.Validators;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Vouchers.RuleEngine;

/// <summary>
/// Engine for validating vouchers based on their configured rules
/// </summary>
public sealed class VoucherRuleEngine : IVoucherRuleEngine
{
    private readonly IVoucherRuleRepository _ruleRepository;
    private readonly Dictionary<string, IVoucherRuleValidator> _validators;

    public VoucherRuleEngine(IVoucherRuleRepository ruleRepository)
    {
        _ruleRepository = ruleRepository;
        _validators = InitializeValidators();
    }

    public async Task<ValidationResult> ValidateAsync(
        VoucherValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetByVoucherIdAsync(
            context.Voucher.VoucherID,
            cancellationToken);

        if (!rules.Any())
        {
            return ValidationResult.Success(context.BookingTotal);
        }

        decimal applicableAmount = context.BookingTotal;
        var applyScopeRuleFound = false;

        foreach (var rule in rules)
        {
            if (!_validators.TryGetValue(rule.RuleType, out var validator))
            {
                return ValidationResult.Failure(
                    rule.RuleType,
                    $"Unknown rule type: {rule.RuleType}");
            }

            var result = validator.Validate(rule, context);

            if (!result.IsValid)
            {
                return result;
            }

            if (rule.RuleType == VoucherRuleTypes.ApplyScope)
            {
                applicableAmount = result.ApplicableAmount;
                applyScopeRuleFound = true;
            }
        }

        if (!applyScopeRuleFound)
        {
            applicableAmount = context.BookingTotal;
        }

        return ValidationResult.Success(applicableAmount);
    }

    private static Dictionary<string, IVoucherRuleValidator> InitializeValidators()
    {
        var validators = new IVoucherRuleValidator[]
        {
            new ApplyScopeValidator(),
            new CinemaValidator(),
            new MovieValidator(),
            new RoomValidator(),
            new SeatTypeValidator(),
            new MembershipValidator(),
            new PaymentMethodValidator(),
            new DayOfWeekValidator(),
            new ProductValidator(),
            new FoodCategoryValidator()
        };

        return validators.ToDictionary(v => v.RuleType, v => v);
    }
}
