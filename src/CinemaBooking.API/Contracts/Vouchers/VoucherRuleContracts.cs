namespace CinemaBooking.API.Contracts.Vouchers;

/// <summary>
/// Request DTO for creating/updating voucher rules
/// </summary>
public sealed record VoucherRuleRequest(
    string RuleType,
    string RuleValue);

/// <summary>
/// Response DTO for voucher rules
/// </summary>
public sealed record VoucherRuleResponse(
    int RuleID,
    string RuleType,
    string RuleValue,
    DateTime CreatedAt);
