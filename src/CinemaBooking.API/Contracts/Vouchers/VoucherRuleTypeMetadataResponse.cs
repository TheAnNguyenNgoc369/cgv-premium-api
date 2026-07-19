namespace CinemaBooking.API.Contracts.Vouchers;

/// <summary>
/// Response DTO describing a single voucher rule type so the admin UI can
/// build its editor dynamically instead of hard-coding rule names.
/// </summary>
/// <param name="RuleType">Rule identifier (matches a <c>VoucherRuleTypes</c> constant).</param>
/// <param name="DisplayName">Human-readable label for the admin UI.</param>
/// <param name="InputType"><c>select</c>, <c>multiselect</c>, <c>text</c>, or <c>number</c>.</param>
/// <param name="DataSource">Absolute API path to fetch options from, when applicable.</param>
/// <param name="Options">Static option list, when applicable.</param>
public sealed record VoucherRuleTypeMetadataResponse(
    string RuleType,
    string DisplayName,
    string InputType,
    string? DataSource,
    IReadOnlyList<string>? Options);
